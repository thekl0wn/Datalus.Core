set nocount on

use master
go

if exists ( select * from sys.databases where name = 'Datalus_db' ) drop database Datalus_db
go

create database Datalus_db
go

use Datalus_db
go

create schema ecp
go
create schema component
go

create table ecp.Entity (
	EntityID int not null
	constraint pk_Entity primary key ( EntityID ) )
go

create table ecp.Component (
	ComponentID int not null identity(1001,1),
	Class varchar(128) not null
	constraint pk_Component primary key ( ComponentID ),
	constraint uq_Component__Class unique ( Class ) )
go

create table ecp.EntityComponent (
	EntityID int not null,
	ComponentID int not null
	constraint pk_EntityComponent primary key ( EntityID, ComponentID ),
	constraint fk_EntityComponent__Entity foreign key ( EntityID ) references ecp.Entity ( EntityID ),
	constraint fk_EntityComponent__Component foreign key ( ComponentID ) references ecp.Component ( ComponentID ) )
go

create table ecp.Manager (
	ManagerID int not null identity(1001,1),
	Class varchar(128) not null
	constraint pk_Manager primary key ( ManagerID ),
	constraint uq_Manager__Class unique ( Class ) )
go

create table ecp.ManagerEntity (
	ManagerID int not null,
	EntityID int not null
	constraint pk_ManagerEntity primary key ( ManagerID, EntityID ),
	constraint fk_ManagerEntity__Manager foreign key ( ManagerID ) references ecp.Manager ( ManagerID ) ,
	constraint fk_ManagerEntity__Entity  foreign key ( EntityID  ) references ecp.Entity  ( EntityID  ) )
go

create table component.Meta (
	EntityID int not null,
	[Name] varchar(64) not null default '',
	[Description] varchar(max) not null default ''
	constraint pk_Meta primary key ( EntityID ),
	constraint fk_Meta__Entity foreign key ( EntityID ) references ecp.Entity ( EntityID ) )
go
	insert ecp.Component select 'MetaComponent'
go

create table component.Health (
	EntityID int not null
	constraint pk_Health primary key ( EntityID ),
	constraint fk_Health__Entity foreign key ( EntityID ) references ecp.Entity ( EntityID ) )
go
	insert ecp.Component select 'HealthComponent'
go

create table component.ListString (
	EntityID int not null,
	ComponentID int not null,
	[Value] varchar(512) not null
	constraint pk_ListString primary key ( EntityID, ComponentID, [Value] ),
	constraint fk_ListString__EntityComponent foreign key ( EntityID, ComponentID ) references ecp.EntityComponent ( EntityID, ComponentID ) )
go

create table component.ListEntity (
	EntityID int not null,
	ComponentID int not null,
	[Value] int not null
	constraint pk_ListEntity primary key ( EntityID, ComponentID, [Value] ),
	constraint fk_ListEntity__EntityComponent foreign key ( EntityID, ComponentID ) references ecp.EntityComponent ( EntityID, ComponentID ),
	constraint fk_ListEntity__Entity foreign key ( [Value] ) references ecp.Entity ( EntityID ) )
go

insert ecp.Component 
		select 'CharacterListComponent'
union	select 'PlayerListComponent'	
go

insert ecp.Manager
		select 'CampaignManager'
union	select 'CharacterManager'
union	select 'PlayerManager'
union	select 'RaceManager'
go

select * from ecp.EntityComponent
select * from ecp.ManagerEntity
