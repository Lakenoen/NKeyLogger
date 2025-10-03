# NkeyLogger
![Static Badge](https://img.shields.io/badge/C%23-Green) ![Static Badge](https://img.shields.io/badge/C%2B%2B-blue) 
![Static Badge](https://img.shields.io/badge/.NET-gray?link=https%3A%2F%2Fdotnet.microsoft.com%2Fen-us) 
![Static Badge](https://img.shields.io/badge/Win32%20API-purple)
![Static Badge](https://img.shields.io/badge/x64-white)
![Static Badge](https://img.shields.io/badge/CMake-orange?label=build)
![Static Badge](https://img.shields.io/badge/Windows-red?label=platform)


## Description
A set of utilities for catching the events of the keyboard and sending them to a remote server with subsequent processing.

## Deployment and using
1. Install the directory with utilities at the link [Tap](https://github.com/lakenoen/nkeylogger/releases)
2. Run the server on a remote machine after installing the [settings](#Server-setting). It starts like a service. Information from the client will be stored in the storage folder in CSV format.
3. Run the client on a host machine after installing the [settings](#Client-setting). It starts like a console app without graphics, it will also write itself in the register for auto loading.
4. Launch ReportMaker to form a report on CSV file [commands](#Report-commands)

## Report commands
- ```.\ReportMaker.exe make <path to csv file> <target path>``` - create report
- ```.\ReportMaker.exe help``` - display a hint

## Server setting
- port = ur port
- cryptoKeySize = Size of crypto key
- maxFileSize = maximum file size  (if it is equal to zero, then the restriction on the size of the CSV file is disabled)

## Client setting
- address = server ip
- port = server port
- reconnect = reconnection time (milliseconds)

