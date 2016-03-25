#!bash

# Run this to automatically overwrite the parts cfg
# in the GameData path

./gen_ls_cfg.pl -f ls_amts.csv > ../GameData/SimpleSurvival/Parts/parts.cfg
echo Done!
