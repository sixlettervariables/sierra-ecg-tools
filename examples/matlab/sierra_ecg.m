%
% Example MATLAB Script to Read Philips Sierra ECG XML Files
%
% Copyright (c) 2014 David D. Salcido and Christopher A. Watford
%
% Permission is hereby granted, free of charge, to any person obtaining a copy
% of this software and associated documentation files (the "Software"), to deal
% in the Software without restriction, including without limitation the rights
% to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
% copies of the Software, and to permit persons to whom the Software is
% furnished to do so, subject to the following conditions:
%
% The above copyright notice and this permission notice shall be included in
% all copies or substantial portions of the Software.
%
% THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
% IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
% FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
% AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
% LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
% OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
% SOFTWARE.

% Written as a standalone script (i.e., not a function) with dependencies
% on 2 publicly available packages:
%  - lzw2norm (included, no license)
%  - base64decode (included, GPLv3 license)
%

clear;
clc;
leads = {}; % This will ultimately hold the extracted data.


% File import
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
[file folder] = uigetfile('*.XML'); %Choose an XML file

% Handles some directory structure pecularities in some systems
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
fslash = strfind(folder,'/');
if isempty(fslash)==1
    dirdemarc = '\';
else
    dirdemarc = '/';
end

slashcheck = strcmp(folder(end),dirdemarc);
if slashcheck == 0
    folder = [folder dirdemarc];
end
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

%
%CAW: Attempts to use xmlread failed under Octave, which appears
% to be a well known issue in the community. For MATLAB users
% this script can be made much more robust by using xmlread()
% and the DOM functions to search for the <parsedwaveforms>
% element.
%
fid = fopen([folder file]); %Asssign file id and open file
data = fscanf(fid,'%c'); %Import as character array

% find boundaries of <parsedwaveforms> element
front_hit_header = strfind(data,'<parsedwaveforms'); %Find starting landmark for 12-Lead Data
grthns = strfind(data,'>'); %In 3 steps, find the end of this XML field
grlocs = find(grthns > front_hit_header); % Step 2...
front_hit = grthns(grlocs(1)); %Step 3...
back_hit = strfind(data,'</parsedwaveforms>'); %Find ending landmark for 12-Lead Data

% extract Base64 encoded data
data = data(front_hit+1:back_hit-1); %Crop 12-Lead Data
data(strfind(data,' ')) = []; %Remove structural spaces (maybe not necessary)
data(strfind(data,sprintf('\n'))) = []; %Remove newlines
data(strfind(data,sprintf('\r'))) = []; %Remove line feeds
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

% Decode from Base64
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
% Note: Use Octave's base64decode
decoded = uint8(base64decode(data));
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

% Extract each of the 12 leads
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
%
leadOffset = 0;
for n = 1:12

    % Extract chunk header
    %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
    % The header is the first 64 bits of each chunk.
    header = decoded(leadOffset+1:leadOffset+8);

    % The size of the ECG data following it is coded in the first 32 bits.
    datasize = typecast(header(1:4), 'uint32');

    % The second part of the header is a 16bit integer of unknown purpose.
    codeone = typecast(header(5:6), 'uint16'); % That integer converted from binary.

    % The last part of the header is a signed 16bit integer that we will use later (delta code #1).
    delta = typecast(header(7:8), 'int16');

    % Now we use datasize above to read the appropriate number of bytes
    % beyond the header. This is encoded ECG data.
    block = uint8(decoded(leadOffset+9:leadOffset+9+datasize-1));
    % assert(datasize == length(block));

    % Convert 8-bit bytes into 10-bit codes (stored in 16-bit ints)
    %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
    % number of 10-bit codes
    codecount = floor((datasize*8)/10);
    codes = zeros(1, codecount, 'uint16');

    offset = 1;
    bitsRead = 0;
    buffer = uint32(0);
    done = false;
    for code = 1:codecount
        % adapted from libsierraecg
        while bitsRead <= 24
          if offset > datasize
              done = true;
              break;
          else
              buffer = bitor(buffer, bitshift(uint32(block(offset)), 24 - bitsRead));
              offset = offset + 1;
              bitsRead = bitsRead + 8;
          end;
        end;

        if done
            break;
        else
            % 32 - codeSize = 22
            codes(code) = uint16(bitand(bitshift(buffer, -22), 65535));
            buffer = bitshift(buffer, 10);
            bitsRead = bitsRead - 10;
        end;
    end;

    % LZW Decompression
    %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
    % Data is compressed with 10-bit LZW codes (last 2 codes are padding)
    [decomp, table] = lzw2norm(codes(1:length(codes)-2));

    %If the array length is not a multiple of 2, tack on a zero.
    if mod(length(decomp),2)~=0
        decomp = [decomp 0];
    end

    % Deinterleave into signed 16-bit integers
    %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
    % The decompressed data is stored [HIWORDS...LOWORDS]
    half = length(decomp)/2;
    output = reshape([decomp(half+1:length(decomp));decomp(1:half)],1,[]);
    output = typecast(output, 'int16');

    % The 16bit ints are delta codes. We now use the delta decoding scheme
    % outlined by Watford to reconstitute the original signal.
    %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
    first = delta;
    prev = first;
    x = output(1);
    y = output(2);
    z = zeros(length(output),1);
    z(1) = x;
    z(2) = y;
    for m = 3:length(output)
        z(m) = (2*y)-x-prev;
        prev = output(m) - 64;
        x = y;
        y = z(m);
    end
    %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

    leads = [leads z];

    % move to the next lead (8 byte header + datasize bytes of payload)
    leadOffset = leadOffset + 8 + datasize;
end;

% Convert leads cell array to numeric matrix
leads = cell2mat(leads);

% Reconstruct Lead III, aVR, aVL, aVF
leads(:,3) = leads(:,2) - leads(:,1) - leads(:,3);
leads(:,4) = -leads(:,4) - (leads(:,1) + leads(:,2))/2;
leads(:,5) = (leads(:,1) - leads(:,3))/2 - leads(:,5);
leads(:,6) = (leads(:,2) + leads(:,3))/2 - leads(:,6);

% 12-Lead Plot
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
figure('numbertitle','off','name','12-Lead Plots');
subplot(6,2,1);
plot((leads(:,1)));
title('I');
subplot(6,2,2);
plot((leads(:,2)));
title('II');
subplot(6,2,3);
plot((leads(:,3)));
title('III');
subplot(6,2,4);
plot((leads(:,4)));
title('aVR');
subplot(6,2,5);
plot((leads(:,5)));
title('aVL');
subplot(6,2,6);
plot((leads(:,6)));
title('aVF');
subplot(6,2,7);
plot((leads(:,7)));
title('V1');
subplot(6,2,8);
plot((leads(:,8)));
title('V2');
subplot(6,2,9);
plot((leads(:,9)));
title('V3');
subplot(6,2,10);
plot((leads(:,10)));
title('V4');
subplot(6,2,11);
plot((leads(:,11)));
title('V5');
subplot(6,2,12);
plot((leads(:,12)));
title('V6');

% Save the 12-Lead data to a CSV file for general analytical purposes
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
[savefile savefolder] = uiputfile('*.csv');

% Handles some directory structure pecularities in some systems
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
fslash = strfind(savefolder,'/');
if isempty(fslash) == 1
    dirdemarc = '\';
else
    dirdemarc = '/';
end

slashcheck = strcmp(savefolder(end),dirdemarc);
if slashcheck == 0
    savefolder = [savefolder dirdemarc];
end
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

csvwrite([savefolder savefile], leads);
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
