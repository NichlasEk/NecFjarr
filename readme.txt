This is a little remote-app for the NEC E558 screens (and alike models). It can be compiled for android and is used to send raw HEX commands to a selected TCP/IP.
You can try to find your NEC screen with the search IP function. This is only tested on my screen and might not work as intended.
The function for switching input however might be useful for someone else. The payload is modifiably, so you can try other commands in code if my commands dosent work.
The Power on and Power off commands are hardcoded raw hex i found on the internet. I have not been avle to create a function with changable payload to try other commands with
the same format. The app is coded in C# with Avalonia UI. 

If you would like to try this for android, i guess you could try my emu.sh if you have an emulator installed and or just my release.sh for compiling and compressing then running
a release. Happy remoting!


inputväxling

Skickat från min Galaxy
poweon

echo '01304130413043024332303344363030303103730D' | xxd -r -p | nc 192.168.32.24 7142

poweroff

echo '01304130413043024332303344363030303403760D' | xxd -r -p | nc 192.168.32.24 7142

vga

echo '0130413045304102303036303030303103730D' | xxd -r -p | nc 192.168.32.24 7142

vga fast component

echo '0130413045304102303036303030304303010D' | xxd -r -p | nc 192.168.32.24 7142

byta till av:

~ $ mono NecSetPayload.exe 0005
Skickar (HEX): 0130413045304102303036303030303503770D
Svar från TV (HEX): 01303041463132023030303036303030303038373030303503090D

HDMI1

$ mono NecSetPayload.exe 0011
Skickar (HEX): 0130413045304102303036303030313203710D
Svar från TV (HEX): 013030414631320230303030363030303030383730303132030F0D

HDMI2

$ mono NecSetPayload.exe 0012
Skickar (HEX): 0130413045304102303036303030313203710D
Svar från TV (HEX): 013030414631320230303030363030303030383730303132030F0D

HDMI3

~ $ mono NecSetPayload.exe 0082
Skickar (HEX): 0130413045304102303036303030383203780D
Svar från TV (HEX): 01303041463132023030303036303030303038373030383203060D
~ $



