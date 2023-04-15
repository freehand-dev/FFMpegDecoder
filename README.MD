# FFMpegDecoder
> FFMpegDecoder is the IP to SDI decoder based on the ffmpeg.

[![.NET](https://github.com/freehand-dev/FFMpegDecoder/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/freehand-dev/FFMpegDecoder/actions/workflows/dotnet.yml)


## Features
- FFmpeg application wrapper
- Decklink output
- FFmpeg progress pipe listener (-progress \\.\pipe\app-guide)
- OpenTelemetry metrics (progress parser)
- Run as Windows Service (multi-instance suppport)


## Compile and install
Once you have installed all the dependencies, get the code:
 ```powershell
git clone https://github.com/freehand-dev/FFMpegDecoder.git
cd FFMpegDecoder
```

Then just use:
```powershell
New-Item -Path "%ProgramData%\FreeHand\FFMpegDecoder\bin\" -ItemType "directory"
dotnet restore
dotnet build
dotnet publish --runtime win-x64 --output %ProgramData%\FreeHand\FFMpegDecoder\bin\ -p:PublishSingleFile=true -p:PublishTrimmed=true -p:PublishReadyToRun=true .\src\FFMpegDecoder
Expand-Archive  .\src\ffmpeg-bin\ffmpeg-decklink.zip %ProgramData%\FreeHand\FFMpegDecoder\bin\ffmpeg-bin\
```

Install as Windows Service (Default instance)
 ```powershell
$params = @{
  Name = "FreeHandFFMpegDecoderSvc_Default"
  BinaryPathName = '"%ProgramData%\FreeHand\FFMpegDecoder\bin\FFMpegDecoder.exe"'
  DisplayName = "FreeHand FFMpegDecoder (Default)"
  StartupType = "Auto"
  Description = "FreeHand FFMpegDecoder (Default)"
}
New-Service @params
```

Install as Windows Service (M1 name instance)
 ```powershell 
$params = @{
  Name = "FreeHandFFMpegDecoderSvc_M1"
  BinaryPathName = '"%ProgramData%\FreeHand\FFMpegDecoder\bin\FFMpegDecoder.exe" -name "M1" --local-config "C:/ProgramData/FreeHand/FFMpegDecoder/custom_config_name.json"'
  DisplayName = "FreeHand FFMpegDecoder (M1)"
  StartupType = "Auto"
  Description = "FreeHand FFMpegDecoder (M1)"
}
New-Service @params
```

Install as Windows Service (M1 name instance)
 ```powershell 
$params = @{
  Name = "FreeHandFFMpegDecoderSvc_M1"
  BinaryPathName = '"%ProgramData%\FreeHand\FFMpegDecoder\bin\FFMpegDecoder.exe" --name "M1"'
  DisplayName = "FreeHand FFMpegDecoder (M1)"
  StartupType = "Auto"
  Description = "FreeHand FFMpegDecoder (M1)"
}
New-Service @params
```

## Configure and start
To start the service, you can use the `FFMpegDecoder` executable as the application or `Start-Service -Name "FreeHandFFMpegDecoderSvc_Default"` as a Windows service. For configuration you can edit a configuration file:

	notepad.exe %ProgramData%\FreeHand\FFMpegDecoder\default.json



### Decoder.Output.Format
- PASS
- UYVY422_1920i50
- UYVY422_720p25
- UYVY422_720p50
- UYVY422_1920p25
- UYVY422_1920p50
- UYVY422_576i50

