# Test cases

## Loading
- [ ] Game loads without crashing
- [ ] Confirm loading of addons in log file
  - [ ] ContractChecker
  - [ ] EVALifeSupportLoader
  - [ ] EVALifeSupportTracker
  - [ ] SimpleSurvivalLoader

## R&D Tech Tree
- [ ] In Science mode:
  - [ ] SimpleSurvival node appears with correct position, icon, description, and cost
  - [ ] Advanced Exploration node is moved to next tier
  - [ ] Arrows flow correctly to/from SimpleSurvival node
  - [ ] Four mod parts appear with correct textures in Interplanetary Habitation
  - [ ] One mod part appears with correct textures in Advanced Exploration
- [ ] In Career mode:
  - [ ] SimpleSurvival node appears with correct position, icon, description, and cost
  - [ ] Advanced Exploration node is moved to next tier
  - [ ] Arrows flow correctly to/from SimpleSurvival node
  - [ ] Four mod parts appear with correct textures in Interplanetary Habitation
  - [ ] One mod part appears with correct textures in Advanced Exploration

## VAB
- [ ] All command modules contain LifeSupport
- [ ] Hitchhiker's module contains LifeSupport
- [ ] Five storage parts contain Consumables and proper descriptions

## Basic Functionality (Resources)
- [ ] LifeSupport does not drain if Kerbal is not present in part
- [ ] LifeSupport does not drain if vessel is in oxygen-rich atmosphere
- [ ] LifeSupport drains once Kerbal leaves oxygen-rich atmosphere
- [ ] LifeSupport drains at correct rate (in Mk3 capsule, 15 days for one Kerbal; 5 days for three Kerbals)
- [ ] When part runs out of LifeSupport, all contained Kerbals are KIA
  - [ ] Other Kerbals in vessel are unharmed
- [ ] EVA LifeSupport is persistent across EVA excursions
- [ ] EVA Propellant is persistent across EVA excursions
- [ ] When part gets low on LifeSupport, warning message is displayed and timescale is set to 1
- [ ] When EVA Kerbal gets low on EVA LifeSupport, warning message is displayed and timescale is set to 1
- [ ] When Kerbal runs out of EVA LifeSupport, Kerbal is KIA

## Basic Functionality (Converter)
- [ ] Converter does not operate if missing an Engineer
- [ ] Converter does not operate if vessel missing Consumables
- [ ] Converter does not operate if vessel missing Electricity
- [ ] With Engineer, Consumables, and Electricity, converter drains Consumables and creates LifeSupport in all valid parts
- [ ] Restricting LifeSupport flow in one part prevents the converter from adding LifeSupport to that part during conversion

## Save/Load
- [ ] Suit LifeSupport values are saved to persistent.sfs when Kerbals are inside a vessel
- [ ] Suit LifeSupport values are loaded correctly when Kerbals are inside a vessel
- [ ] EVA LifeSupport+Propellant values are properly saved to persistent.sfs
- [ ] EVA LifeSupport+Propellant values are properly loaded


## Contracts
- [ ] Rescue contract vessels are successfully loaded with LifeSupport when they come into focus in the Flight scene
- [ ] Confirm that stale contract GUIDs are pruned from persistent.sfs

## Config
- [ ] Confirm that EVA LifeSupport values upgrade according to Astronaut Complex level
- [ ] Confirm that EVA Propellant values upgrade according to Astronaut Complex level
- [ ] Test EVA LifeSupport upgrade behavior:
  - [ ] Recovered: Kerbal must be recovered to update
  - [ ] IfHitchhiker (default): Kerbal can board any vessel with Hitchhiker module
  - [ ] Aboard: Kerbal can board any vessel
- [ ] Test EVA Propellant refill behavior:
  - [ ] Recovered: Kerbal must be recovered to update
  - [ ] IfHitchhiker (default): Kerbal can board any vessel with Hitchhiker module
  - [ ] Aboard: Kerbal can board any vessel
- [ ] Test converter requirements:
  - [ ] None: Converter can be used, even if unmanned
  - [ ] AnyKerbal: Converter can be manned by any Kerbal
  - [ ] Engineer (default): Converter must be manned by an Engineer
  - [ ] IfKerbalExpEnabled: Converter must be manned by Engineer if exp is enabled, otherwise any Kerbal will do

## GUI
- [ ] Correctly shows state of LifeSupport consumption (inactive, breathable air, or draining)
- [ ] Shows Consumables remaining in time
- [ ] Minimize button works
- [ ] Size increase/decrease button works
- [ ] EVA LifeSupport refill button does not work if no Consumables
- [ ] EVA LifeSupport refill button does not work if Kerbal's EVA LS is already full
- [ ] EVA LifeSupport refill button works when above conditions are met
- [ ] Confirm that Kerbals with net life support of <30 seconds can only move when "allow unsafe behavior" is checked

## Misc
- [ ] SimpleSurvival correctly initializes vessels and Kerbals when loading for the first time into a save which did not use the mod
- [ ] A vessel left alone and with enough Consumables should automatically drain Consumables upon reload, and not drain Electricity (to be consistent with vanilla behavior)
- [ ] A vessel left alone and without Consumables should run out of LifeSupport and kill the Kerbals
