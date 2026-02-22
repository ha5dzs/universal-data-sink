# universal-data-sink

This code dumps incoming UDP packets to a user-specified file with timestamps

## How to use

* Start the program (`this_repo\Universal Data Sink\bin\Debug\net8.0\datasink.exe`).
* Send the _absolute_ path of the file _with respect to the computer the code is running on_ as plain text payload to UDP port `3430` of the computer the code is running on
  * The code validates the path, and throws an exception if the path is not valid.
* Send whatever data you want to UDP port `3431` of the computer the code is running on

Data will be stored as:
`<UTC unix time in milliseconds>,<the payload of the packet directly as you sent it><0x0A>`

## Why

Sometimes I occasionally need to log some real-time data, with as little disruption as possible. Connection-less asynchronous communication seems to be the best solution so far.

## Things to know

* Intended for Windows, with dotnet 8 runtime installed
* Every data packet is _appended_ to the file asynchronously. Ideally nothing gets overwritten, and the file is closed after every operation
* If you 'forget' specifying the file name, look for a `.csv` file in the directory the executable is running in.
* I haven't really stress-tested this, but it's not really for brutal data loads.
