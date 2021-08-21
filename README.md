# VORP Mailbox

A resource for RedM which allows players to send and receive Letters like mails. 

# Installation 

**Dependencies**

- [VORP-Core](https://github.com/VORPCORE/VORP-Core)
- [VORP-Character](https://github.com/VORPCORE/VORP-Character)
- [ghmattimysql](https://github.com/GHMatti/ghmattimysql/releases)

**Instructions**

- Extract vorp_mailbox into your resources folder
- Import mailbox.sql into your database 
- Add the following line to your server.cfg file:
```cfg
ensure vorp_mailbox
```

## Configuration

You can add new locations of mailbox by amending the locations field in the Config.json file.
No need to recompile the project, juste restart the resource after updating the Config.

You can also change language between french and english by updating the same Config.json file


This resource has been created for Nolosha.
