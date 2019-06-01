@echo off

echo.
echo.
echo     ##    ###   #   #      ###    ##   #####   ##        ###   ####  ##   #  (Parallel)
echo    #  #  #      #   #      #  #  #  #    #    #  #      #      #     # #  #
echo    #      ##     # #       #  #  ####    #    ####      # ##   ###   #  # #
echo    #  #     #    # #       #  #  #  #    #    #  #      #  #   #     #   ##
echo     ##   ###      #        ###   #  #    #    #  #       ##    ####  #    #
echo.
echo.

if %1==help goto help
if %1==? goto help

if not exist %1 goto error

:execute

  set numOfLines=0
  
  color 0e

  for /f %%i in ('find "" /v /c ^< %1')	do set /a numOfLines=%%i
  
  if %numOfLines% GTR %NUMBER_OF_PROCESSORS% goto warning
  goto startloop
 
  :warning
    echo WARNING!!!  You are executing more processes than the current number of cores on this machine.  Performance may be degraded.
    pause

  :startloop
    
	for /f "tokens=1,2,3 delims=," %%i in (%1) do start csvdatagen -n%%i -o%%j -i%%k %2 %3 %4 %5 %6 %7 %8 %9

  color 0a
  goto end

:help
  
  echo.
  csvdatagen /?
  echo.
  echo ----------------------------------------------------------------------------------------------------------------------------------------------------------------
  echo.
  echo 1. DESCRIPTION
  echo.
  echo    P-CSVDATAGEN is a batch file that wraps around csvdatagen.exe to launch multiple instances of the process for process parallelization.  The main executable,
  echo    csvdatagen.exe, is single threaded.  For large data sets, you should partition the data you need to create into separate datasets using separate
  echo    format files allowing you to run multiple processes in parallel with p-csvdatagen.  Each JSON format file used by csvdatagen will align with one process in
  echo    the parallelization of your workload.  The partition file you pass to p-csvdatagen should be a CSV file that contains 1 row for each partition and format file
  echo    that describes the dataset to be created.  A separate process will be created for each row in the partition file.  The format of the p-csvdatagen partition file (csv)
  echo    is as follows:
  echo.
  echo.      [file naming convention],[output directory],[input format file]
  echo.
  echo       With:
  echo          [file naming convention] = This tag will be prepended to each file name for partition uniqueness allowing all files to be written to the same directory.
  echo                                     This tag should be unique in the csv partition file for each row.   You may however, provide separate directories for each 
  echo                                     partition which, if the directories have separate I/O paths, will improve performance.
  echo          [output directory]       = This is the output location to write the csv files generated for the partition.  With unique file naming convention tags, all
  echo                                     files can be written to the same directory.  For large datasets, separate I/O paths will improve performance.
  echo          [input format file]      = The JSON format file that describes how to create the CSV data file for this partition.
  echo.
  echo 2. BATCH FILE USAGE: 
  echo.
  echo       p-csvdatagen [path to partition file (see above)] [param 1] [param 2] [param 3] [param n]
  echo.
  echo    For parallel execution, you may provide additional parameters after the path to the partition file.
  echo.
  echo       For example:
  echo.
  echo       p-csvdatagen c:\temp\processlist.txt -C**
  echo.
  echo       **When launching parallel instances of csvdatagen, the cmd window will close once csvdatagen terminates.  To leave them open so 
  echo         results and stats can be read, pass the -C parameter.  More parameters are available by running csvdatagen /?.
  echo.
  echo    NOTE: For parallel execution, the tag in the partition file should be unique for *each* row and partition.
  echo.
  echo    WARNING: For parallel execution, you should not execute more processes (rows in the partition file) than the number of cores available on the machine.
  echo.
  echo.
  goto end

:error
 
  color 0c
  echo.
  echo.
  echo [p-csvdatagen]: Partition file was not found.  The partition file *must* be the first parameter.
  echo.

:end