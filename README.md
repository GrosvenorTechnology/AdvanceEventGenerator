# AdvanceEventGenerator

This tool is designed as a sample tool for sending event traffic to a API for testing.

It allows you to configure multiple devices and users in the appsettings.json file, the 
runner will then randomly send a mix of the defined users to the Portals for each device

## Boot config settings

Use this section to set the base url for sending events, the default `device/{deviceSerial}/events` path will
be appended to the default URI.  You can change this behavior in the worker.cs SendMessage function.

> You must change the DefaultUri to match your configuration

```json
{
   "BootConfig": {
      "Boot": {
         "DefaultUri": "http://127.0.0.1:8080/grosvenor-oem"
      }
   }
}
```

## Request Settings

This can be used to set the delay between requests sent for each device.  The actual time used will be
the `RequestDelayMs` (time in milliseconds) plus a random value between 0 and `JitterMs`.  This is to 
add some randomness to the traffic being sent.

```json
{
   "Requests": {
      "RequestDelayMs": 5000,
      "JitterMs":  1000
   }
}
```

## Device Settings

This section allows you to define the devices you want to use to send traffic, their portals and associated readers.

```json
{
   "DeviceConfig": {
      "Devices": [
         {
            "SerialNumber": "OEM-ADV-C2-MLT~00000005",
            "SharedKey": "9nF2W3A18UG8XOGI7gsk2UV+CdpsSCZ3YHGvQjkKtKY=",
            "Portals": [
               {
                  "PortalId": "101",
                  "ReaderIds": [ "101", "102" ]
               },
               {
                  "PortalId": "201",
                  "ReaderIds": [ "201", "202" ]
               }
            ]
         },
         {
            "SerialNumber": "OEM-ADV-C2-MLT~00000006",
            "SharedKey": "9nF2W3A18UG8XOGI7gsk2UV+CdpsSCZ3YHGvQjkKtKY=",
            "Portals": [
               {
                  "PortalId": "101",
                  "ReaderIds": [ "101", "102" ]
               },
               {
                  "PortalId": "201",
                  "ReaderIds": [ "201", "202" ]
               }
            ]
         }
      ]
   }
}
```

## User Settings

This setting allows you to define your users

```json
{
    "UsersConfig": {
      "Users": [
         {
            "UserId": "123456",
            "TokenId": "987654",
            "TokenData": "0000357951"
         },
         {
            "UserId": "456789",
            "TokenId": "654321",
            "TokenData": "0000159753"
         }
      ]
   }
}
```


## Running in docker

The simple way to run the application is to run it as a docker image

> You must override the default URI

```
docker run -it -e BootConfig__Boot__DefaultUri='http://192.168.43.101:8080/grosvenor-oem' grosvenortechnology/advance-event-generator
```

To supply your configuration of users and devices you can use a volume mount to inject the configuration.
Using this method, you can remove the device and user config from the `appsettings.json` file and
create separate `users.json` and `devices.json` files.  You can then use a volume mount to pass these
to the docker container.  In this example my files are in `c:\tmp\config`

```
docker run -it -v c:\tmp\config:/config -e BootConfig__Boot__DefaultUri='http://192.168.43.101:8080/grosvenor-oem' grosvenortechnology/advance-event-generator
```

### Building docker image

> You should be in the /src folder of the git repo to run this command

```
docker build -f .\AdvanceEventGenerator\Dockerfile -t eventgen:latest .
```

You can then run with the command

```
docker run -it eventgen:latest
```