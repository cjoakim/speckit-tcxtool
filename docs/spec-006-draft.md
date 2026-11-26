# Specification 006: Draft

## Overview

This spec will enhance and expand the TCX-to-CSV parsing process
with three main features:
- Enhanced Current CSV File
- New States CSV File
- New States Page

## Enhanced Current CSV File

Enhance the current output CSV file as follows:

- Double the value of the "RunCadence" column
  - For example, a TCX value of 60 will be 120 in the output CSV
- Add a new column called "State"
  - The value should default to "unknown"
  - The value is "stopped" if the RunCadence is 0
  - The value is "walking" if the RunCadence is between 1 and 149
  - The value is "running" if the RunCadence is 150 or greater
- Add a new column called "StateSeconds"
  - This is the number of seconds since the above new `State` column changed
- Locate the State and StateSeconds columns after the RunCadence column

## New States CSV File

This file will be produced from the above enhanced CSV file.

It's filename will be the same as the enhanced CSV file, but with the extension ".states.csv".

Add key `statesCsvFilename` to the tcxtool-state.json file when this new CSV
file is created.

This file will have a header row, and one data row which summarizes each state 
change in the first CSV file.  Your logic is to read the first CSV file row-by-row.
When you detect that the value of the "State" column changes, you should write
a new row to the new States CSV file that summarizes that `stopped` or `walking` 
or `running` state.

For example, if the first 38 rows of the first CSV file are in the "walking" state,
and the next 113 rows are in the "running" state, you should write one row to the States CSV file
that summarizes the "walking" state (rows 1-38), and one row that summarizes the "running" state (rows 39-151).

Lets call the set of rows in each state a "StateSet" (i.e. - rows 1-38, and rows 39-151).

This file will have the following columns:
- ActivityId
- StateEndDate - the Date of the last row in the StateSet
- StateEndTrackpointNum  - the TrackpointNum of the last row in the StateSet
- StateElapsedSeconds - the number of seconds in that StateSet from start to finish 
- ActivityElapsedTime - the same value as the ElapsedTime column in the first CSV file
- AverageLatitude - the average Latitude values for the rows in the StateSet
- AverageLongitude - the average Longitude values for the rows in the StateSet
- AverageHeartRateBpm - the average HeartRateBpm values for the rows in the StateSet
- AverageRunCadence - the average RunCadence values for the rows in the StateSet
- DistanceMiles - the distance in miles traveled for the rows in the StateSet
- AverageAltitudeMeters - the average AltitudeMeters values for the rows in the StateSet
- AverageAltitudeFeet - the average AltitudeFeet values for the rows in the StateSet

## New States Page

In the UI, add a new page called "States".

It will appear after "Table" in the navigation menu.

It will display a table of all rows and all columns of the States CSV file.

The cell background color will use the same logic as the Table page.
