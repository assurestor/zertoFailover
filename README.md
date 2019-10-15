# zertoFailover
Command Line utility to automate test and live failovers for Zerto environments

## Getting Started
The Zerto Failover utility makes use of the supplied `com.assurestor.api.vendor.dll` file for all API calls to the local ZVM, please ensure this is included in all builds of the code.

## Prerequisites
The utility makes use of a CSV file containing a list of the VPG/s to be failed over for testing or a live failover, along with any pre-defined pauses, an example csv file is included in the code directory called `example.csv`

- **BuildGroup** = not really used at the moment, should be a number that is greater than 0

- **VpgName** = The VPG name

- **Delay** = the number of seconds you wish to delay/pause before moving onto the next line
`Note. The header line must exist in the CSV file otheriwse it will error.`

## How to Use
To run the utility you must provide the location of the CSV file and also the run mode (start or stop). These can be supplied using the command line parameters below.

**To start a failover test using the csv file example.csv**
`zertoFailover.exe -c .\example.csv -m start`

**To stop a failover test using the csv file example.csv**
`zertoFailover.exe -c .\example.csv -m stop`

In addition you can also specify:
- **failoverType** = If this is a test or live failover `**WARNING Live failover will shutdown the source VM if it is running, use with caution!**`

- **commitPolicy** = for live failover's only, will set the commit policy as either rollback, commit or none. If set as rollback or commit this action will be performed automatically after the waitTime has expired

- **waitTime** = the number of seconds to wait before processing the specified commitPolicy (rollback or commit)

**To start a live failover using the csv file example.csv with a commit policy ste to rollback after 30 minutes (1800 seconds)**
`zertoFailover.exe -c .\example.csv -m start -f live -p rollback -t 1800`

## Command Line Parameters
-c, --csv             Required. point to the csv file containing vm/vpg build details

-m, --mode            Required. specify if the script should start or stop the failover (start | stop)

-f, --failoverType    (Default: test) specify if the script should perform a test or live failover (test | live)

-p, --commitPolicy    (Default: rollback) the policy to use after the failover enters a 'Before Commit' state (rollback | commit | none)

-t, --waitTime        (Default: 3600) the amount of time in seconds the failover waits in a 'Before Commit' state before processing the commitPolicy

--help                Display this help screen.

--version             Display version information.

## Contributing
1. Fork it!
2. Create your feature branch: `git checkout -b my-new-feature`
3. Commit your changes: `git commit -am 'Add some feature'`
4. Push to the branch: `git push origin my-new-feature`
5. Submit a pull request :D

## History
15/10/2019: Initial release of AssureStor Zerto Failover Utility
