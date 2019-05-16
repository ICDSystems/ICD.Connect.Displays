# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [11.1.3] - 2019-05-16
### Changed
 - PanasonicClassicDisplay driver queries power and input states
 - PanasonicClassicDisplay driver parses volume feedback

## [11.1.2] - 2019-02-28
### Changed
 - Fixed Panasonic Classic volume feedback parsing

## [11.1.1] - 2019-01-28
### Changed
 - Failing gracefully when no SerialQueue is assigned to displays
 - Fix for NullRef in PanasonicClassicDisplay

## [11.1.0] - 2019-01-15
### Added
 - Added Barco VideoWall display driver

## [11.0.0] - 2019-01-02
### Added
 - Added mute console commands to displays

### Changed
 - Renamed DisplayScreenRelayControl to RelayProjectorScreen

## [10.1.0] - 2018-11-20
### Added
 - Added screen relay device

### Chaged
 - Fixed issues with SonyBravia feedback parsing
 - Fixed Christie Projector parsing

## [10.0.0] - 2018-11-13
### Added
 - Added Crestron Connect Display

### Changed
 - Removed InputCount and HdmiInput properties, displays properly support sparse addressing

## [9.0.0] - 2018-10-30
### Added
 - Added SonyBraviaDisplay

### Changed
 - Renamed Microsoft.SurfaceHub project to Microsoft
 - Fixed volume ramping for sharp displays
 - Only query display state on port assignment if the port is connected
 - Query displays when port becomes connected

## [8.0.1] - 2018-09-14
### Changed
 - Fixed dependencies on volume controls

## [8.0.0] - 2018-09-14
### Added
 - Added additional inputs to SamsungProDisplay
 - Added Sharp prosumer display

### Changed
 - Fixed NEC wall id not being saved properly
 - Fixed bug where SamsingProDisplay would sometimes power back on by itself
 - Significant routing performance improvements

## [7.2.2] - 2018-07-02
### Changed
 - Fixed SamsungProDisplay not correctly pulling WallId from settings

## [7.2.1] - 2018-06-19
### Changed
 - Fixes for SharpPro control over TCP

## [7.2.0] - 2018-06-04
### Changed
 - Serial devices use ConnectionStateManager for maintaining connection to remote endpoints
 - SPlus shim improvements
 - Sharp warmup fix

## [7.1.0] - 2018-05-24
### Changed
 - Fixed AbstractSPlusDisplayShim TOriginator constraint
 - SPlus shim improvements
 - Potential Samsung warmup fix

## [7.0.0] - 2018-05-09
### Added
 - Added display property setters for S+

### Changed
 - Fixed control id overlap
 - Changed S+ shim naming convention

## [6.1.0] - 2018-05-03
### Added
 - Adding power control to displays

## [6.0.0] - 2018-04-27
### Changed
 - SPlus displays use new API events

## [5.0.0] - 2018-04-23
### Added
 - Adding API proxies for displays
 - Adding API attributes to display interfaces
 - Adding SPlus display interfaces
 - Adding Simpl display interfaces
 - Adding Simpl display device
 - Adding Simpl display with audio device
 
### Removed
 - Removed old, unused SPlus display interface
 - Removing unused VolumeControl property from IDisplayWithAudio

### Changed
 - Fixed the command retry in Sharp displays
 - Changed the input set command to set input after successful poll where input is different from requested - fixes warmup and input select on some sharp consumer series
 - Changing display events to inherit from API event args
