#!perl

use warnings;
use strict;

use Getopt::Long;

my $LS_RESOURCE = "LifeSupport";
my $LS_MODULE = "LifeSupportModule";
my $CONV_MODULE = "Cons2LSModule";

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
    next if ($line =~ /Notes/);

    if ($line =~ /^([^,]*),([^,]*),([^,]*),([^,]*),([^,]*),/)
    {
        # Cfg part name
        my $name = $2;
        # Will this part include a converter?
        my $converter = $3;
        # Amount of LifeSupport
        my $ls_amt = $5;

        next unless ($converter || $ls_amt);

        print "\@PART[$name]\n";
        print "{\n";

        if ($ls_amt)
        {
            print "    RESOURCE\n";
            print "    {\n";
            print "        name = $LS_RESOURCE\n";
            print "        amount = $ls_amt\n";
            print "        maxAmount = $ls_amt\n";
            print "    }\n";
            print "    MODULE\n";
            print "    {\n";
            print "        name = $LS_MODULE\n";
            print "    }\n";
        }

        if ($converter)
        {
            print "    MODULE\n";
            print "    {\n";
            print "        name = $CONV_MODULE\n";
            print "    }\n";
        }

        print "}\n\n";
    }
}

close $FILE;

