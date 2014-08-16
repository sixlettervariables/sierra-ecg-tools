C=gcc
CFLAGS=-c -Wall `xml2-config --cflags`
LDFLAGS=`xml2-config --libs`
SOURCES=main.c sierraecg.c lzw.c b64.c
OBJECTS=$(SOURCES:.c=.o)
EXECUTABLE=sierraecg-test

all: $(SOURCES) $(EXECUTABLE)
	
$(EXECUTABLE): $(OBJECTS) 
	$(C) $(LDFLAGS) $(OBJECTS) -o $@

.c.o:
	$(C) $(CFLAGS) $< -o $@
