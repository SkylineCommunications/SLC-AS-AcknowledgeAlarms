# SLC-AS-AcknowledgeAlarms

Automation script to acknowledge one or multiple alarms in DataMiner.

## Overview

This repository contains a DataMiner Automation Script project (`DataMinerType=AutomationScript`) targeting **.NET Framework 4.8**.

The script accepts alarm identifiers via the `alarms` script parameter and acknowledges the specified alarms. It also handles correlated alarms where applicable.

## Parameter format

The script expects the `alarms` parameter in this format:

`dataminerId&&elementId&&alarmId&&rootId`

For multiple alarms, use `+` to separate values per field:

`dma1+dma2&&elem1+elem2&&alarm1+alarm2&&root1+root2`

All `+`-separated lists must contain the same number of items.

## Requirements

- DataMiner version: **10.4.0.0 - 14003** or higher
- .NET Framework: **4.8**
- Visual Studio with DataMiner SDK support

## Build

Open the solution in Visual Studio and build:

- Solution: `Acknowledge Alarms/Acknowledge Alarms.slnx`
- Project: `Acknowledge Alarms/Acknowledge Alarms.csproj`

## NuGet package

Main dependency:

- `Skyline.DataMiner.Dev.Automation` (10.6.9.1)
