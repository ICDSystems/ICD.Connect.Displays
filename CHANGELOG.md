# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [6.0.0] - 2018-04-27
### Changed
 - SPlus displays use new API events

## [5.0.0] - 2018-04-23
### Added
 - Adding API proxies for displays
 - Adding API attributes to display interfaces
 
### Removed
 - Removed old, unused SPlus display interface
 - Removing unused VolumeControl property from IDisplayWithAudio

### Changed
 - Fixed the command retry in Sharp displays
 - Changed the input set command to set input after successful poll where input is different from requested - fixes warmup and input select on some sharp consumer series
 - Changing display events to inherit from API event args
 - Adding SPlus display interfaces
 - Adding Simpl display interfaces
 - Adding Simpl display device
 - Adding Simpl display with audio device
