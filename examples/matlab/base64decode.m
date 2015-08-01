%  Copyright (C) 2007 Muthiah Annamalai <muthiah.annamalai@uta.edu>
% 
%  This program is free software; you can redistribute it and/or modify it under
%  the terms of the GNU General Public License as published by the Free Software
%  Foundation; either version 3 of the License, or (at your option) any later
%  version.
% 
%  This program is distributed in the hope that it will be useful, but WITHOUT
%  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
%  FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more
%  details.
% 
%  You should have received a copy of the GNU General Public License along with
%  this program; if not, see <http://www.gnu.org/licenses/>.

%  -*- texinfo -*-
%  @deftypefn {Function File} {@var{rval} =} base64decode (@var{code})
%  @deftypefnx {Function File} {@var{rval} =} base64decode (@var{code}, @var{as_string})
%  Convert a base64 @var{code}  (a string of printable characters according to RFC 2045) 
%  into the original ASCII data set of range 0-255. If option @var{as_string} is 
%  passed, the return value is converted into a string.
% 
%  @example
%  @group
%            ##base64decode(base64encode('Hakuna Matata'),true)
%            base64decode('SGFrdW5hIE1hdGF0YQ==',true)
%            ##returns 'Hakuna Matata'
%  @end group
%  @end example
% 
%  See: http://www.ietf.org/rfc/rfc2045.txt
% 
%  @seealso {base64encode}
%  @end deftypefn

function z = base64decode (X, as_string)
  isOctave = exist("OCTAVE_VERSION") != 0;

  if (nargin < 1 )
    print_usage;
  elseif nargin == 1
    as_string=false;
  end

  if (any(X(:) < 0) || any(X(:) > 255))
    error("base64decode is expecting integers in the range 0 .. 255");
  end

  %  decompose strings into the 4xN matrices 
  %  formatting issues.
  if (size(X,1) == 1)
    Y=[];
    L=length(X);
    for z=4:4:L
        Y=[Y X(z-3:z)']; #keep adding columns
    end
    if min(size(Y))==1
        Y=reshape(Y,[L, 1]);
    else
        Y=reshape(Y,[4,L/4]);
    end
    X=Y;
    Y=[];
  end

  if (isOctave)
    X=toascii(X);
  else
    X=X - 0; % hack for MATLAB, ASCII to decimal conversion
  end
  Xa=X;

  %  Work backwards. Starting at step in table,
  %  lookup the index of the element in the table.

  %  6-bit encoding table, plus 1 for padding
  %  26*2 + 10 + 2 + 1  = 64 + 1, '=' is EOF stop mark.
  table = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";

  S=size(X);
  SRows=S(1);
  SCols=S(2);
  Y=zeros(S);

  %  decode the incoming matrix & 
  %  write the values into Va matrix.
  Va = -1*ones(size(Xa));

  iAZ = (Xa >= 'A').*(Xa <= 'Z') > 0; 
  Va(iAZ)=Xa(iAZ)-'A';

  iaz = (Xa >= 'a').*(Xa <= 'z') > 0;
  Va(iaz)=Xa(iaz)-'a'+26;

  i09 = (Xa >= '0').*(Xa <= '9') > 0;
  Va(i09)=Xa(i09)-'0'+52;

  is = (Xa == '/') ;  Va(is) = 63;
  ip = (Xa == '+') ;  Va(ip) = 62;
  ieq = (Xa == '=') ;  Va(ieq) = 0;
  clear is; clear ieq; clear ip; clear i09;
  clear iaz; clear iAZ;  clear Xa; clear X;

  Y=Va; clear Va;
  Y1=Y(1,:);
  if (SRows > 1)
     Y2=Y(2,:);
  else 
     Y2=zeros(1,SCols);
  end;

  if (SRows > 2)
     Y3=Y(3,:);
  else 
     Y3=zeros(1,SCols);
  end;

  if (SRows > 3)
     Y4=Y(4,:);
  else 
     Y4=zeros(1,SCols);
  end;

  %  +1 not required due to ASCII subtraction
  %  actual decoding work
  b1 = Y1*4 + fix(Y2/16);
  b2 = mod(Y2,16)*16+fix(Y3/4);
  b3 = mod(Y3,4)*64 + Y4;

  ZEROS=sum(sum(Y==0));
  L=length(b1)*3;
  z=zeros(1,L);
  z(1:3:end)=b1;
  if (SRows > 1)
     z(2:3:end)=b2;
  else
     z(2:3:end)=[];
  end;

  if (SRows > 2)
     z(3:3:end)=b3;
  else
     z(3:3:end)=[];
  end

  %  FIXME: is this expected behaviour?
  if (as_string)
    L=length(z);
    while ( ( L > 0) && ( z(L)==0 ) )
      L=L-1;
    end
    z=char(z(1:L));
  end

end %function

%!assert(base64decode(base64encode('Hakuna Matata'),true),'Hakuna Matata')
%!assert(base64decode(base64encode([1:255])),[1:255])
%!assert(base64decode(base64encode('taken'),true),'taken')
%!assert(base64decode(base64encode('sax'),true),'sax')
%!assert(base64decode(base64encode('H'),true),'H')
%!assert(base64decode(base64encode('Ta'),true),'Ta')
