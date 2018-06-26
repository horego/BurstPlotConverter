# BurstPlotConverter
Burstcoin PoC1 to PoC2 Plot Converter

Hey Burstcoin friends,

here is my own PoC1 to PoC2 converter with modern c# features.
I focused on inline conversion performance.
If you like my work I will bring this converter to linux (.net core) as well.
For **Windows** systems i added **support for conversion while mining**. Explained below.

## Usage
If you execute the console app without any information you will see the help page:
>       Horego Burst Plot Converter 1.0.0.0
>       Copyright c  2018
>       ERROR(S):
>       No verb selected.
>         inline     Inline plot conversion.
>         outline    Sepeate output file plot conversion.
>         info       Plot and program information.
you can i.e  type Horego.BurstPlotConverter.exe inline for detailed command information.

## Convert while mining on the same HDD (Only Windows)
I added this feature to support mining and converting plots on the same hard disk.

The assumption of this feature is that there is only one process running (namely the miner) which cause high disk usage.
This means if the miner is running the disk usage increase and the conversion is paused as long the miner is working. When the miner finished the conversion is resumed automatically. The distinction between the miner is working or not is done by a parameter called "-threshold [in Mb/sec]". If the disk usage is above this value the conversion is running. Else the miner will pause until the value is above the threshold again.

The restrictions are:
* Only one unique process (miner) can be wached.
* If the process reads/writes an different drive your conversion will also be paused and resumed.
* If the process reads/writes the drive you are converting and also reading other drives then the conversion will resume when the last drive is finished (defined by the threshold). 
## Example
* Show details on used memory and plot information with requested memorysize of 2000MB
> Horego.BurstPlotConverter.exe info -r H:\xxxx_171966464_393216_393216 -m 2000
* Perform inline conversion
> Horego.BurstPlotConverter.exe inline -r H:\xxxx_171966464_393216_393216 -m 2000
* Perform outline conversion
> Horego.BurstPlotConverter.exe outline -r H:\xxxx_171966464_393216_393216 -w G:\xxxx_171966464_393216 -m 2000
* Abort conversion
> You can abort the conversion with ctrl + c or ctrl + break. When you wait a little bit the application notifies you about your resume point.
> * Abort requested. Please wait until conversion has been safely aborted.
> * Aborted conversion. You can safely resume conversion with:
> * -c 125829120
> Press enter to exit.
* Resume conversion
> After the cancellation simply append the parameter -c 125829120 to your command line
> Horego.BurstPlotConverter.exe inline -r H:\xxxx_171966464_393216_393216 -m 2000 -c 125829120
* Convert while mining on the same HDD
> Horego.BurstPlotConverter.exe inline -r H:\xxxx_171966464_393216_393216 -m 2000 -w blagominer_avx -t 10
> * The Process name (balgominer_avx) must be entered without the file extension (.exe)
> * The threshold (10 mb/sec) in megabytes/sec is defined by -t
## Performance
The performance is different based on your system. It took about 35 minutes for me with inline conversion for one 100GB file with the following system:
* WD Red 6TB (SATA II)
* 1536Mb Memory usage
* CPU i7-2600 (Sandy-Bridge)

## Support
If you like my work donate me some burst to
> BURST-ZH3A-9L8Z-BEUV-5QLSD

:) Thanks to PoC-Consortium for the reference implementation [source](https://github.com/PoC-Consortium/Utilities/tree/master/poc3proto.pl "Source").
