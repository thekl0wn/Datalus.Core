set nocount on

use master
go

if exists ( select * from sys.databases where name = 'Datalus_db' ) drop database Datalus_db
go

create database Datalus_db
go

use Datalus_db
go

create schema entity
go
create schema cmp
go
create schema unit
go

create table entity.Entity (
	EntityCode varchar(32) not null,
	RepositoryCode varchar(32) not null
	constraint pk_Entity primary key ( EntityCode ) )
go
	insert entity.Entity select 'all',  'all'
	insert entity.Entity select 'null', 'all'
go

create table entity.Component (
	ComponentCode varchar(32) not null,
	Class varchar(128) not null,
	[Table] varchar(64) not null
	constraint pk_Component primary key ( ComponentCode ),
	constraint uq_Component__Class unique ( Class ) )
go

create table entity.EntityComponent (
	EntityCode varchar(32) not null,
	ComponentCode varchar(32) not null
	constraint pk_EntityComponent primary key ( EntityCode, ComponentCode ),
	constraint fk_EntityComponent__Entity foreign key ( EntityCode ) references entity.Entity ( EntityCode ),
	constraint fk_EntityComponent__Component foreign key ( ComponentCode ) references entity.Component ( ComponentCode ) )
go

create table unit.[Type] (
	TypeCode varchar(32) not null,
	[Name] varchar(64) not null,
	[Table] varchar(64) not null
	constraint pk_UnitType primary key ( TypeCode ) )
go

create table unit.[Master] (
	UnitCode varchar(32) not null,
	Abbreviation varchar(8) not null,
	TypeCode varchar(32) not null,
	[Name] varchar(64) not null
	constraint pk_UnitMaster primary key ( UnitCode ),
	constraint uq_UnitMaster_Abbreviation unique ( Abbreviation ) ,
	constraint fk_UnitMaster_UnitType foreign key ( TypeCode ) references unit.[Type] ( TypeCode ) )
go

create table unit.Conversion (
	FromCode varchar(32) not null,
	ToCode varchar(32) not null,
	Ratio numeric(20,8) not null
	constraint pk_UnitConversion primary key ( FromCode, ToCode ),
	constraint fk_UnitConversion_From foreign key ( FromCode ) references unit.[Master] ( UnitCode ) ,
	constraint fk_UnitConversion_To   foreign key ( ToCode   ) references unit.[Master] ( UnitCode ) )
go

create table unit.[Count] (
	UnitCode varchar(32) not null
	constraint pk_UnitCount primary key ( UnitCode ),
	constraint fk_UnitCount_Unit foreign key ( UnitCode ) references unit.[Master] ( UnitCode ) )
go

create table unit.Currency (
	UnitCode varchar(32) not null
	constraint pk_UnitCurrency primary key ( UnitCode ),
	constraint fk_UnitCurrency_Unit foreign key ( UnitCode ) references unit.[Master] ( UnitCode ) )
go

create table unit.Distance (
	UnitCode varchar(32) not null
	constraint pk_UnitDistance primary key ( UnitCode ),
	constraint fk_UnitDistance_Unit foreign key ( UnitCode ) references unit.[Master] ( UnitCode ) )
go

create table unit.StatType (
	TypeCode varchar(32) not null,
	[Name] varchar(64)   not null
	constraint pk_StatType primary key ( TypeCode ) )
go

create table unit.Stat (
	StatCode varchar(32) not null,
	StatType varchar(32) not null,
	[Name]   varchar(64) not null,
	Calculation varchar(128) not null,
	ParentStatCode varchar(32) not null
	constraint pk_UnitStat primary key ( StatCode ) )
go

create table unit.[Time] (
	UnitCode varchar(32) not null
	constraint pk_UnitTime primary key ( UnitCode ),
	constraint fk_UnitTime_Unit foreign key ( UnitCode ) references unit.[Time] ( UnitCode ) )
go

create table unit.Volume (
	UnitCode varchar(32) not null
	constraint pk_UnitVolume primary key ( UnitCode ),
	constraint fk_UnitVolume_Unit foreign key ( UnitCode ) references unit.[Master] ( UnitCode ) )
go

create table unit.[Weight] (
	UnitCode varchar(32) not null
	constraint pk_UnitWeight primary key ( UnitCode ),
	constraint fk_UnitWeight_Unit foreign key ( UnitCode ) references unit.[Master] ( UnitCode ) )
go

create table cmp.Meta (
	EntityCode varchar(32) not null,
	[Name] varchar(64) not null default '',
	[Description] varchar(max) not null default ''
	constraint pk_Meta primary key ( EntityCode ),
	constraint fk_Meta__Entity foreign key ( EntityCode ) references entity.Entity ( EntityCode ) )
go

create table cmp.Characters (
	EntityCode varchar(32) not null,
	CharacterCode varchar(32) not null
	constraint pk_Characters primary key ( EntityCode, CharacterCode ),
	constraint fk_Characters_Entity foreign key ( EntityCode ) references entity.Entity ( EntityCode ),
	constraint fk_Characters_Character foreign key ( CharacterCode ) references entity.Entity ( EntityCode ) )
go

create table cmp.Concentration (
	EntityCode varchar(32) not null,
	SpellCode  varchar(32) not null
	constraint pk_Concentration primary key ( EntityCode ),
	constraint fk_Concentration_Entity foreign key ( EntityCode ) references entity.Entity ( EntityCode ) ,
	constraint fk_Concentration_Spell  foreign key ( SpellCode  ) references entity.Entity ( EntityCode ) )
go

create table cmp.Dice (
	EntityCode varchar(32) not null,
	DieCode   varchar(32) not null,
	[Count]    int         not null
	constraint pk_Dice primary key ( EntityCode, DieCode ),
	constraint fk_Dice_Entity foreign key ( EntityCode ) references entity.Entity ( EntityCode ) ,
	constraint fk_Dice_Die    foreign key ( DieCode    ) references entity.Entity ( EntityCode ) )
go

create table cmp.DungeonMaster (
	EntityCode varchar(32) not null,
	PlayerCode varchar(32) not null
	constraint pk_DungeonMaster primary key ( EntityCode ),
	constraint fk_DungeonMaster_Entity foreign key ( EntityCode ) references entity.Entity ( EntityCode ) ,
	constraint fk_DungeonMaster_Player foreign key ( PlayerCode ) references entity.Entity ( EntityCode ) )
go

create table cmp.Duration (
	EntityCode varchar(32) not null,
	[Value] int not null,
	UnitCode varchar(32) not null
	constraint pk_Duration primary key ( EntityCode ),
	constraint fk_Duration_Entity foreign key ( EntityCode ) references entity.Entity ( EntityCode ) ,
	constraint fk_Duration_Unit   foreign key ( UnitCode   ) references unit.[Time]   ( UnitCode   ) )
go

create table cmp.Height (
	EntityCode varchar(32) not null,
	[Value] numeric(20,8) not null,
	UnitCode varchar(32) not null
	constraint pk_Height primary key ( EntityCode ),
	constraint fk_Height_Entity foreign key ( EntityCode ) references entity.Entity   ( EntityCode ) ,
	constraint fk_Height_Unit   foreign key ( UnitCode   ) references unit.[Distance] ( UnitCode   ) )
go

create table cmp.Ingredients (
	EntityCode varchar(32) not null,
	ItemCode   varchar(32) not null,
	Quantity   numeric(20,8) not null,
	UnitCode   varchar(32) not null,
	Consumed   bit not null default 1
	constraint pk_Ingredients primary key ( EntityCode, ItemCode ),
	constraint fk_Ingredients_Entity foreign key ( EntityCode ) references entity.Entity ( EntityCode ) ,
	constraint fk_Ingredients_Item   foreign key ( ItemCode   ) references entity.Entity ( EntityCode ) ,
	constraint fk_Ingredients_Unit   foreign key ( UnitCode   ) references unit.[Master] ( UnitCode   ) )
go

create table cmp.Inventory (
	EntityCode varchar(32) not null,
	ItemCode   varchar(32) not null,
	Quantity   numeric(20,8) not null,
	UnitCode   varchar(32) not null
	constraint pk_Inventory primary key ( EntityCode, ItemCode ),
	constraint fk_Inventory_Entity foreign key ( EntityCode ) references entity.Entity ( EntityCode ) ,
	constraint fk_Inventory_Item   foreign key ( ItemCode   ) references entity.Entity ( EntityCode ) ,
	constraint fk_Inventory_Unit   foreign key ( UnitCode   ) references unit.[Master] ( UnitCode ) )
go

create table cmp.Items (
	EntityCode varchar(32) not null,
	ItemCode   varchar(32) not null
	constraint pk_Items primary key ( EntityCode, ItemCode ),
	constraint fk_Items_Entity foreign key ( EntityCode ) references entity.Entity ( EntityCode ) ,
	constraint fk_Items_Item   foreign key ( ItemCode   ) references entity.Entity ( EntityCode ) )
go

create table cmp.[Length] (
	EntityCode varchar(32) not null,
	[Value] numeric(20,8) not null,
	UnitCode varchar(32) not null
	constraint pk_Length primary key ( EntityCode ),
	constraint fk_Length_Entity foreign key ( EntityCode ) references entity.Entity   ( EntityCode ) ,
	constraint fk_Length_Unit   foreign key ( UnitCode   ) references unit.[Distance] ( UnitCode   ) )
go

create table cmp.[Owner] (
	EntityCode varchar(32) not null,
	OwnerCode  varchar(32) not null
	constraint pk_Owner primary key ( EntityCode ),
	constraint fk_Owner_Entity foreign key ( EntityCode ) references entity.Entity ( EntityCode ) ,
	constraint fk_Owner_Owner  foreign key ( OwnerCode  ) references entity.Entity ( EntityCode ) )
go

create table cmp.Players (
	EntityCode varchar(32) not null,
	PlayerCode varchar(32) not null
	constraint pk_Players primary key ( EntityCode, PlayerCode ),
	constraint fk_Players_Entity foreign key ( EntityCode ) references entity.Entity ( EntityCode ) ,
	constraint fk_Players_Player foreign key ( PlayerCode ) references entity.Entity ( EntityCode ) )
go

create table cmp.Recipes (
	EntityCode varchar(32) not null,
	RecipeCode varchar(32) not null
	constraint pk_Recipes primary key ( EntityCode, RecipeCode ),
	constraint fk_Recipes_Entity foreign key ( EntityCode ) references entity.Entity ( EntityCode ) ,
	constraint fk_Recipes_Recipe foreign key ( RecipeCode ) references entity.Entity ( EntityCode ) )
go

create table cmp.Sides (
	EntityCode varchar(32) not null,
	Sides      int         not null
	constraint pk_Sides primary key ( EntityCode ),
	constraint fk_Sides_Entity foreign key ( EntityCode ) references entity.Entity ( EntityCode ) )
go

create table cmp.Spells (
	EntityCode varchar(32) not null,
	SpellCode  varchar(32) not null
	constraint pk_Spells primary key ( EntityCode, SpellCode ),
	constraint fk_Spells_Entity foreign key ( EntityCode ) references entity.Entity ( EntityCode ) ,
	constraint fk_Spells_Spell  foreign key ( SpellCode  ) references entity.Entity ( EntityCode ) )
go

create table cmp.SpellSlots (
	EntityCode varchar(32) not null,
	[Level] int not null,
	[Count] int not null,
	Used int not null
	constraint pk_SpellSlots primary key ( EntityCode, [Level] ),
	constraint fk_SpellSlots_Entity foreign key ( EntityCode ) references entity.Entity ( EntityCode ) )
go

create table cmp.[Stats] (
	EntityCode varchar(32) not null,
	StatCode varchar(32) not null,
	[Value] int not null,
	[Proficiency] numeric(2,1)
	constraint pk_Stats primary key ( EntityCode, StatCode ),
	constraint fk_Stats_Entity foreign key ( EntityCode ) references entity.Entity ( EntityCode ) ,
	constraint fk_Stats_Stat   foreign key ( StatCode   ) references unit.Stat     ( StatCode   ) )
go

create table cmp.UnitType (
	EntityCode varchar(32) not null,
	TypeCode   varchar(32) not null
	constraint pk_UnitType primary key ( EntityCode ),
	constraint fk_UnitType_Entity foreign key ( EntityCode ) references entity.Entity ( EntityCode ) ,
	constraint fk_UnitType_Type   foreign key ( TypeCode   ) references unit.[Type]   ( TypeCode   ) )
go

create table cmp.[Weight] (
	EntityCode varchar(32) not null,
	[Value] numeric(20,8) not null,
	UnitCode varchar(32) not null
	constraint pk_Weight primary key ( EntityCode ),
	constraint fk_Weight_Entity foreign key ( EntityCode ) references entity.Entity ( EntityCode ) ,
	constraint fk_Weight_Unit   foreign key ( UnitCode   ) references unit.[Weight] ( UnitCode   ) )
go

create table cmp.Width (
	EntityCode varchar(32) not null,
	[Value] numeric(20,8) not null,
	UnitCode varchar(32) not null
	constraint pk_Width primary key ( EntityCode ),
	constraint fk_Width_Entity foreign key ( EntityCode ) references entity.Entity   ( EntityCode ) ,
	constraint fk_Width_Unit   foreign key ( UnitCode   ) references unit.[Distance] ( UnitCode   ) )
go

create table cmp.Worth (
	EntityCode varchar(32) not null,
	[Value] numeric(20,8) not null,
	UnitCode varchar(32) not null
	constraint pk_Worth primary key ( EntityCode ),
	constraint fk_Worth_Entity foreign key ( EntityCode ) references entity.Entity ( EntityCode ) ,
	constraint fk_Worth_Unit   foreign key ( UnitCode   ) references unit.Currency ( UnitCode   ) )
go

create table cmp.Yield (
	EntityCode varchar(32) not null,
	[Value] numeric(20,8) not null,
	UnitCode varchar(32) not null
	constraint pk_Yield primary key ( EntityCode ),
	constraint fk_Yield_Entity foreign key ( EntityCode ) references entity.Entity ( EntityCode ) ,
	constraint fk_Yield_Unit   foreign key ( UnitCode   ) references unit.[Master] ( UnitCode   ) )
go


insert entity.Component
	  select 'dm', 'DungeonMasterComponent', '[cmp].[DungeonMaster]'
go

insert unit.StatType
      select 'skill',   'Skill'
union select 'ability', 'Ability'
union select 'passive', 'Passive Skill'
union select 'none',    '<NONE>'
go

insert unit.Stat select 'none', 'none', '<NONE>', '', 'none'
go
alter table unit.Stat add constraint fk_UnitStat_Parent foreign key ( ParentStatCode ) references unit.Stat ( StatCode )
go
insert unit.Stat 
      select 'STR', 'ability', 'Strength',     '', 'none'
union select 'DEX', 'ability', 'Dexterity',    '', 'none'
union select 'CON', 'ability', 'Constitution', '', 'none'
union select 'INT', 'ability', 'Intelligence', '', 'none'
union select 'WIS', 'ability', 'Wisdom',       '', 'none'
union select 'CHA', 'ability', 'Charisma',     '', 'none'
go
insert unit.Stat
      select 'acrobatics',      'skill', 'Acrobatics',      '', 'DEX'
union select 'animal-handling', 'skill', 'Animal Handling', '', 'WIS'
union select 'arcana',          'skill', 'Arcana',          '', 'INT'
union select 'athletics',       'skill', 'Athletics',       '', 'STR'
union select 'deception',       'skill', 'Deception',       '', 'CHA'
union select 'history',         'skill', 'History',         '', 'INT'
union select 'insight',         'skill', 'Insight',         '', 'WIS'
union select 'intimidation',    'skill', 'Intimidation',    '', 'CHA'
union select 'investigation',   'skill', 'Investigation',   '', 'INT'
union select 'medicine',        'skill', 'Medicine',        '', 'WIS'
union select 'nature',          'skill', 'Nature',          '', 'INT'
union select 'perception',      'skill', 'Perception',      '', 'WIS'
union select 'performance',     'skill', 'Performance',     '', 'CHA'
union select 'persuasion',      'skill', 'Persuasion',      '', 'CHA'
union select 'religion',        'skill', 'Religion',        '', 'INT'
union select 'sleight-of-hand', 'skill', 'Sleight of Hand', '', 'DEX'
union select 'stealth',         'skill', 'Stealth',         '', 'DEX'
union select 'survival',        'skill', 'Survival',        '', 'WIS'

union select 'passive-perception', 'passive', 'Passive Perception', '10 + MODIFIER( [perception] )', 'none'
union select 'proficiency',        'passive', 'Proficiency',        '',                              'none'
go

insert unit.[Type]
      select 'distance', 'Distance', '[unit].[Distance]'
union select 'currency', 'Currency', '[unit].[Currency]'
union select 'count',    'Count',    '[unit].[Count]'
union select 'weight',   'Weight',   '[unit].[Weight]'
union select 'volume',   'Volume',   '[unit].[Volume]'
union select 'time',     'Time',     '[unit].[Time]'
go

insert unit.[Master]
      select 'ea', 'ea', 'count', 'Each'

union select 'cp', 'cp', 'currency', 'Copper Pieces'
union select 'sp', 'sp', 'currency', 'Silver Pieces'
union select 'gp', 'gp', 'currency', 'Gold Pieces'
union select 'pp', 'pp', 'currency', 'Platinum Pieces'

union select 'lbs', 'lbs', 'weight', 'Pounds'
union select 'oz',  'oz',  'weight', 'Ounces'
union select 'ton', 'ton', 'weight', 'Tons'

union select 'hr',    'hr',    'time', 'Hours'
union select 'min',   'min',   'time', 'Minutes'
union select 'sec',   'sec',   'time', 'Seconds'
union select 'round', 'round', 'time', 'Rounds'
union select 'day',   'day',   'time', 'Days'
union select 'week',  'wk',    'time', 'Weeks'
union select 'yr',    'yr',    'time', 'Years'

union select 'in', 'in', 'distance', 'Inches'
union select 'ft', 'ft', 'distance', 'Feet'
union select 'yd', 'yd', 'distance', 'Yards'
union select 'mi', 'mi', 'distance', 'Miles'

union select 'gal',  'gal',     'volume', 'Gallons'
union select 'qt',   'qt',      'volume', 'Quarts'
union select 'pt',   'pt',      'volume', 'Pints'
union select 'floz', 'fl. oz.', 'volume', 'Fluid Ounces'
go

insert unit.[Count]
      select 'ea'
go

insert unit.Distance
      select 'in'
union select 'ft'
union select 'yd'
union select 'mi'
go

insert unit.[Time]
      select 'sec'
union select 'round'
union select 'min'
union select 'hr'
union select 'day'
union select 'week'
union select 'yr'
go

insert unit.[Volume]
      select 'gal'
union select 'qt'
union select 'pt'
union select 'floz'
go

insert unit.[Weight]
      select 'lbs'
union select 'ton'
union select 'oz'
go

insert unit.Conversion
	  select 'lbs',   'oz',          16.00000000
union select 'lbs',   'ton',          0.00050000
union select 'oz',    'lbs',          0.06250000
union select 'oz',    'ton',          0.00003125
union select 'ton',   'lbs',       2000.00000000
union select 'ton',   'oz',       32000.00000000

union select 'gal',   'qt',           4.00000000
union select 'gal',   'pt',           8.00000000
union select 'gal',   'floz',       128.00000000
union select 'qt',    'gal',          0.25000000
union select 'qt',    'pt',           2.00000000
union select 'qt',    'floz',        32.00000000
union select 'pt',    'qt',           0.50000000
union select 'pt',    'gal',          0.12500000
union select 'pt',    'floz',        16.00000000
union select 'floz',  'gal',          0.00781250
union select 'floz',  'qt',           0.03125000
union select 'floz',  'pt',           0.06250000

union select 'in',    'ft',           0.08333333
union select 'in',    'yd',           0.02777778
union select 'in',    'mi',           0.00001578
union select 'ft',    'in',          12.00000000
union select 'ft',    'yd',           0.33333333
union select 'ft',    'mi',           0.00018939
union select 'yd',    'ft',           3.00000000
union select 'yd',    'in',          36.00000000
union select 'yd',    'mi',           0.00056818
union select 'mi',    'yd',        1760.00000000
union select 'mi',    'ft',        5280.00000000
union select 'mi',    'in',       63360.00000000

union select 'sec',   'min',          0.01666667
union select 'sec',   'hr',           0.00277778
union select 'sec',   'round',        0.16666667
union select 'sec',   'day',          0.00001157
union select 'sec',   'week',         0.00000165
union select 'sec',   'yr',           0.00000003
union select 'round', 'sec',          6.00000000
union select 'round', 'min',          0.10000000
union select 'round', 'hr',           0.00166667
union select 'round', 'day',          0.00006944
union select 'round', 'week',         0.00000992
union select 'round', 'yr',           0.00000019
union select 'min',   'hr',           0.01666667
union select 'min',   'sec',         60.00000000
union select 'min',   'round',       10.00000000
union select 'min',   'day',          0.00069444
union select 'min',   'week',         0.00009921
union select 'min',   'yr',           0.00000190
union select 'hr',    'min',         60.00000000
union select 'hr',    'sec',       3600.00000000
union select 'hr',    'round',      600.00000000
union select 'hr',    'day',          0.04166667
union select 'hr',    'week',         0.00575238
union select 'hr',    'yr',           0.00011416
union select 'day',   'sec',      86400.00000000
union select 'day',   'round',    14400.00000000  
union select 'day',   'min',       1440.00000000
union select 'day',   'hr',          24.00000000
union select 'day',   'week',         0.14285714
union select 'day',   'yr',           0.00273973
union select 'week',  'day',          7.00000000
union select 'week',  'hr',         168.00000000
union select 'week',  'min',      10080.00000000
union select 'week',  'round',   100800.00000000
union select 'week',  'sec',     604800.00000000
union select 'week',  'yr',           0.01917808
union select 'yr',    'week',        52.14285714
union select 'yr',    'day',        365.00000000
union select 'yr',    'hr',        8760.00000000
union select 'yr',    'min',     525600.00000000
union select 'yr',    'round',  5256000.00000000
union select 'yr',    'sec',   31536000.00000000

union select 'cp',    'sp',           0.10000000
union select 'cp',    'gp',           0.01000000
union select 'cp',    'pp',           0.00100000
union select 'sp',    'cp',          10.00000000
union select 'sp',    'gp',           0.10000000
union select 'sp',    'pp',           0.01000000
union select 'gp',    'cp',         100.00000000
union select 'gp',    'sp',          10.00000000
union select 'gp',    'pp',           0.10000000
union select 'pp',    'cp',        1000.00000000
union select 'pp',    'sp',         100.00000000
union select 'pp',    'gp',          10.00000000
go