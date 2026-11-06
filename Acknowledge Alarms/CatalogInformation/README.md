# Acknowledge Alarms

## About

Acknowledge Alarms is a DataMiner automation script that acknowledges supplied alarms and, for correlation alarms, their referenced base alarms. It lets callers handle a single alarm or a batch in one execution.

## Key Features

- Acknowledge one or multiple alarms using a single script parameter.
- Attempt to acknowledge referenced base alarms before the main alarm when its source is "Correlation Engine".
- Continue processing after individual DataMiner COM acknowledgment failures and log the affected alarms.

## Prerequisites

- DataMiner version 10.4.0.0 - 14003 or higher, as declared by the project.
- Permission to execute the script and acknowledge the target alarms.
- Valid identifiers for the alarms to acknowledge.

## Input

Supply the string parameter `alarms` using four fields separated by `&&`:

```text
dataminerId&&elementId&&alarmId&&rootId
```

Single-alarm example:

```text
101&&2001&&3001&&3001
```

For multiple alarms, separate the identifiers within each field with `+`. All four fields must contain the same number of identifiers, in matching order.

```text
101+101&&2001+2002&&3001+3002&&3001+3002
```

All identifiers must be integers. The `rootId` field is required and parsed, but is not used to retrieve or acknowledge alarms. The script identifies each target using `dataminerId`, `elementId`, and `alarmId`.

## Behavior and Limitations

The script retrieves each target alarm's details. If its source is "Correlation Engine" and base-alarm references are available, it attempts to acknowledge those alarms first, using the comment `Acknowledged by {treeId}`. It then acknowledges the main alarm with the comment `Acknowledged via script`.

DataMiner COM exceptions during acknowledgment are logged. A failed base-alarm acknowledgment does not prevent attempts on the remaining base alarms or the main alarm. A failed main-alarm acknowledgment does not prevent processing the remaining supplied alarms.

Other errors, including invalid input or failure to retrieve alarm details, fail execution. Earlier acknowledgments are not rolled back. Completion without a script error does not guarantee that every alarm was acknowledged; check the script logs for individual failures.
