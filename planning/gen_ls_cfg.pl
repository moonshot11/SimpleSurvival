#!perl

use warnings;
use strict;

use Getopt::Long;

my $LS_RESOURCE = "LifeSupport";
my $LS_MODULE = "LifeSupportModule";
my $CONV_MODULE = "Cons2LSModule";

my $NL = "\r\n";

# Input filename
my $arg_file = "";

&GetOptions(
    "f=s", \$arg_file
);

die "-f <filename> required!\n" unless ($arg_file);
die "File $arg_file does not exist!\n" unless (-e $arg_file);

open my $FILE, "<", $arg_file;

while (my $line = <$FILE>)
{
    chomp $line;

    # Skip if this is the header
    next if ($line =~ /Notes/ || $line =~ /^[\s,]*$/);

    if ($line =~ /^([^,]*),([^,]*),([^,]*),([^,]*),([^,]*),([^,]*),([^,]*),/)
    {
        # Cfg part name
        my $name = $2;
        # Will this part include a converter?
        my $converter = $3;
        # Amount of LifeSupport
        my $ls_amt = $5;
        # Technode to change, if any
        my $technode = $7;

        next unless ($name);
        next unless ($converter || $ls_amt || $technode);

        print "\@PART[$name]$NL";
        print "{$NL";

        if ($technode)
        {
            print "    \@TechRequired = $technode$NL";
        }

        if ($ls_amt)
        {
            print "    RESOURCE$NL";
            print "    {$NL";
            print "        name = $LS_RESOURCE$NL";
            print "        amount = $ls_amt$NL";
            print "        maxAmount = $ls_amt$NL";
            print "    }$NL";
            print "    MODULE$NL";
            print "    {$NL";
            print "        name = $LS_MODULE$NL";
            print "    }$NL";
        }

        if (0 && $converter)
        {
            print "    MODULE$NL";
            print "    {$NL";
            print "        name = $CONV_MODULE$NL";
            print "    }$NL";
        }

        print "}$NL$NL";
    }
}

close $FILE;


