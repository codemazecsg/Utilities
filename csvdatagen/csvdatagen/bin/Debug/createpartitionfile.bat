@echo off

if "%1"=="" goto error

if %1==help goto help
if %1==? goto help

if "%2"=="" goto error
if "%3"=="" goto error
if "%4"=="" goto error

:begin

  set loop=0
  
    color 0e
	:loop
	
	echo P%loop%,%2,%3_%loop%.json >> %4
	
	set /a loop=%loop%+1

	if %loop% LSS %1 goto loop
	color 0a
	goto end
	
:help

  echo.
  echo CREATEPARTITIONFILE.BAT
  echo -----------------------------------------------------------------------------------------------------------------------------------------------
  echo.
  echo This script will create a partition file for use with p-csvdatagen.bat which is used for running csvdatagen.exe in parallel.  You must provide
  echo all parameters.  The script will create a partition file with the number of partitions (entries) specified.  The script requires that you use the
  echo same output directory, but will automatically name the partition tags using P#.  It will append the partition number to the end of your input file
  echo parameter.  Therefore you must provide *ONLY* the stub of your input file as detailed below.
  echo.
  echo    USAGE:  createpartitionfile [number of partitions] [output directory] [input format file names without # or extension] [Full path and name of partition file to create]
  echo.
  echo    PARAMETERS:
  echo.
  echo            [number of partitions]:          This is the # of paritions (rows) written to the file which will result in 1 process of csvdatagen each.
  echo            [output directory]:              This is the output directory where the output of csvdatagen will be written.
  echo                                             For automatic creation of the partition file the output directories must all be the same but you can edit
  echo                                             the partition file after creation.
  echo            [input file name stub]:          Your input format files must be created with the *SAME* name in the same directory with an ordinal number at the 
  echo                                             end in the format of _#.json.   For instance, a properly named input format file would be c:\temp\my_input_file_0.json.
  echo                                             All the input format files would be the named the same with a different numerical suffix (i.e. _1, _2, etc...).  You 
  echo                                             would then pass the above file naming convention into the tool as c:\temp\my_input_file.
  echo            [full path and name]:            This is the full path and name of the partition file to create.  You can use a .txt extension.
  echo.
  goto end

:error

  color 0c
  echo.
  echo You must provide all parameters for partition file creation.  For help, run createpartitionfile ?.
  echo.

:end