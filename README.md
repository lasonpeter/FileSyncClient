# FileSync (Client)
#### Simple file sync implementation in .NET
# Documentation
Documentation is available [here](https://lasonpeter.github.io/FileSyncServer/)

# How to use
Make sure you have already set up a *Server* component of *FileSync* or have access credentials to already available one
## Installation
### Ubuntu
    In the future there will be a ready to use package available
### Arch
    In the future there will be a ready to use package in aur repository
----

### Other
- Go to [releases](https://github.com/lasonpeter/FileSyncClient/releases) and pick the appropriate build for your operating system and processor architecture
- Unpack the contents into a directory of your choice, ***WARNING !*** make sure it's not in the directory that is being watched, or it will cause an ***infinite synchronization loop***

## Configuration
Inside of `config.json` set the appropriate credentials for connection with *FileSync* server
```json
{
  "port": 11000,
  "host_name": "localhost", 
  "synchronization_paths": [
    {
      "path": "/home/xenu/Documents/",
      "recursive": true,
      "synchronized": true  
    }
  ],
  "working_directory": ""
}
```
Then inside of `synchronization_paths` put list of paths that you want to synchronize, 
just like in the example above

## Run it!
That's all, now run the program, and that's all.

---

# TODO

- [ ] Stream compression
- [ ] Client-server authentication
- [ ] Bidirectional file synchronization (currently its only client -> server)
- [ ] File conflict resolution
- [ ] Partial file synchronization


## Licensing

This project uses the [MIT](https://opensource.org/license/MIT) license

---
## WARNING
The Project is W.I.P, and as of now it's *usable** but not recommended,
as a result there will be no precise documentation until the first stable release
which is projected to happen around November 2024

---
Project uses its own protocol library which currently is in its infancy: [TransferLib](https://github.com/lasonpeter/TransferLib)
