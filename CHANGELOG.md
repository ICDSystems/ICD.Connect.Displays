# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Changed
 - AbstractDisplayWithAudio add HandleIsMutedChanged protected virtual method

## [15.5.0] - 2022-07-01
### Added
 - Added SamsungFrameDisplay with Art Mode options

### Changed
 - SamsungPro Added disable option for Launcher and SystemInfo telemetry
 - Added virtual HandlePowerStateChange method to AbstractDisplay
 - Updated Crestron SDK to 2.18.96

## [15.4.4] - 2022-05-23
### Changed
 - Added default network port for Barco Video Wall Display

## [15.4.3] - 2021-12-29
### Changed
 - Fix issue with PanasonicClassicDisplay where input changes would not be retried during the display warming period.

## [15.4.2] - 2021-10-04
### Changed
 - Fix an issue with LG displays where power save mode would cause an inaccurate tracked power state.

## [15.4.1] - 2021-07-26
### Changed
 - Fixed a bug where SamsingProDisplay would error out on failed queries instead of retrying the query
 - Added default IP ports for NEC, Panasonic, Samsung Pro, and Sharp displays

## [15.4.0] - 2021-05-14
### Added
 - Samsung Display - added support for URL Launcher input & implemented queries for gathering additional telemetry
 
### Changed
 - Reducing Sharp keep-alive interval to 1 minute, potential fix for connection drops
 - Fixed conversion exception in proxy display
 - Fixed a bug where the display console did not work for proxy displays

## [15.3.0] - 2021-02-10
### Added
 - Samsung Display - added support for AV, SVideo, Composite, TV Tuner inputs

### Changed
 - Samsung Display - better handling of any unsolicited responses

## [15.2.0] - 2021-02-05
### Changed
 - Fully implemented RelayDisplayLiftDevice CopySettings method.
 - PanasonicDisplay now retries failed/timed-out commands
 - Mock displays start in PoweredOff state

## [15.1.2] - 2020-09-24
### Changed
 - Fixed a bug where default display activities were not being initialized

## [15.1.1] - 2020-08-13
### Changed
 - Telemetry namespace change

## [15.1.0] - 2020-07-14
### Changed
 - AbstractProxySimplDisplaySettings now inherits from AbstractProxyDeviceSettings instead of AbstractProxySettings
 - Simplified external telemetry providers

## [15.0.0] - 2020-06-18
### Changed
 - MockDisplayWithAudio now inherits from AbstractMockDevice
 - Using new logging context

### Removed
 - Removed Aspect settings from IDisplay, and removed all aspect controls from all displays

## [14.1.2] - 2021-03-08
### Changed
 - Fixed issues with SamsungProDisplays over IP control where a disconnect on power on prevented input select commands from working

## [14.1.1] - 2020-10-12
### Changed
 - Removed legacy factory name from RelayProjectorScreen since it was preventing the current factory name from working

## [14.1.0] - 2020-10-06
### Changed
 - Implemented StartSettings() to start communications with devices
 - Cleaned up AbstractProjectorScreenDevice to use new LifecycleState

## [14.0.1] - 2020-06-16
### Changed
 - PanasonicClassicDisplay - Fixed power state feedback issues
 - PanasonicClassicDisplay - Fixed volume ramping issues

## [14.0.0] - 2020-03-20
### Added
 - Added Planar QE display driver

### Changed
 - Displays round volume level on assignment instead of flooring
 - Reworked displays to fit new volume interfaces
 - NEC Projector now handles unknown responses without breaking
 - Epson Project will now ignore unsolicited feedback
 - Fixed default baud rate for NEC projectors

## [13.0.0] - 2019-12-12
### Added
 - AbstractProjector implements IProjector, supports lamp hours and expected warming/cooling durations
 - AbstractDisplay calls RaisePowerStateChanged to allow ExpectedDuration to be implemented
 - IProjector now uses int API event args for OnLampHoursUpdated, with ApiEvent decorator
 - IProjector now uses int property for LampHours, with ApiProperty decorator
 - EpsonProjector implements iProjector with LampHours support

### Changed
 - Fixed IndexOutOfRange exception for NEX display power feedback handling
 - Fixed Sony Bravia feedback issues related to "success" messages
 - Fixed issues with RS-232 control on EpsonProjector
 - Fixed NEC monitor ID offset

## [12.4.0] - 2019-11-19
### Added
 - Epson Projector Driver - "EpsonProjector", tested against Epson PowerLite X39
 - NEC Projector Driver - "NecProjector", tested against NEC P525UL
 
### Changed
 - AbstractDisplay PortOnIsOnlineStateChanged and PortOnConnectedStateChanged methods are now protected virtual
 - Fixed mute toggle bug with Sharp prosumer displays
 - Fixed power feedback bug with Sharp prosumer displays

## [12.3.0] - 2019-10-07
### Changed
 - Fixed a bug where volume control was not working due to power state
 - Added ExpectedDuration to DisplayPowerStateApiEventArgs
 - MockDisplayWithAudio starts powered off
 - MockDisplayWithAudio will set last requested input, volume, etc when powered

## [12.2.0] - 2019-09-16
### Changed
 - Updated IPowerDeviceControls to use PowerState
 - IrProjectorScreenDevice changed "IrPort" element to "Port" for consistency
 - SamsungDisplay removed first command suffix (doesn't seem to be needed)
 - SamsungDisplay fixed string comparison problems on Mono platform preventing power off feedback from working

## [12.1.1] - 2019-08-15
### Changed
 - Fixes for Samsung consumer display

## [12.1.0] - 2019-01-29
### Added
 - Added LG DigitalSignage display driver

### Changed
 - Fixed bug that was preventing MockDisplayWithAudio from powering on

## [12.0.0] - 2019-01-10
### Added
 - Implementing port configuration features for display devices

### Changed
 - MockDisplayWithAudio no longer inheriting from serial display implementations

## [11.6.0] - 2020-05-06
### Added
 - Added ISamsungProCommand to handle commands and queries
 
### Changed
 - AbstractSamsungProDisplay - fixed issue with generics in SendCommand and comparer
 - AbstractSamsungProDisplay - fixed null ref in command comparer

## [11.5.2] - 2020-03-03
### Changed
 - PanasonicDisplay - fixed issue with parsing responses from polling that don't include the original command

## [11.5.1] - 2020-02-14
### Changed
 - Using new SerialQueue with rate limiting

## [11.5.0] - 2020-02-03
### Added
 - Added SamsungProVideoWall display
 
### Changed
 - Created AbstractSamsungProDisplay to support both SamsungProDisplay and SamsungProVideoWallDisplay

## [11.4.2] - 2019-12-12
### Changed
 - Sharp Consumer and Prosumer - wait after setting display power states to poll, so the display returns the correct state

## [11.4.1] - 2019-10-29
### Changed
  - On SamsungProDisplay, only send an unmute command with the volume change if the tv is already muted.
  - Fix issue where PanasonicClassicDisplay did not call base when configuring port

## [11.4.0] - 2019-09-03
### Changed
 - Added additional input options to Christie J-Series

## [11.3.1] - 2019-08-15
### Changed
 - Fixed a bug where the Christie J-Series driver was not correctly parsing active input

## [11.3.0] - 2019-07-16
### Added
 - Added all input addresses for the Microsoft SurfaceHub display
 - Added Display Lift abstractions, interfaces, telemetry and console
 - Added RelayDisplayLiftDevice

## [11.2.0] - 2019-05-16
### Added
 - Added telemetry features to displays

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
