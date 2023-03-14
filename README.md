# StatLoggingPlugin
An [AssettoServer](https://github.com/compujuckel/AssettoServer) plugin made for Cruisin' USA. Check out the [Cruisin' USA Discord](https://discord.gg/5yYBCukqce).

This plugin was created using YhugiLS' [LightSpeedPlugin](https://github.com/YhugiLS/LightspeedPlugin) as a starting point. Please check his work out as well as his [Discord](https://discord.gg/dnzdf5E7Zb).

### What does this plugin do?
This plugin creates a sqlite DB where it logs players stats when they leave. This is a work in progress and may contain bugs. Its intended use is for Cruisin' USA Servers.
 - SteamID
 - Name
 - Model
 - Skin
 - KMDriven
 - TimeSpent
 - TopSpeed
 - Environment Collisions
 - Traffic Collisions
 - Player Collisions
 - Major Accidents(Collisions over 150kph relative speed)
 - Server Specified in extra_cfg.yml

## Configuration
```yaml
---
!StatLoggingConfiguration
# Use a common DB file for multiple servers.
# If not set database will be created in server directory
CommonDB: true
# Relative or Absolute path of DB
# Example - 'C:\Users\assetto\Documents\commonserverfolder\'
# Example - '..\'
CommonDBFileLocation: '../'
# Name of server. Will be used in the last column of DB.
ServerName: 'lac-de-2'
```
###### Cruisin USA
