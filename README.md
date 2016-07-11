[![Build status](https://ci.appveyor.com/api/projects/status/5jkoijqetsqmpaii?svg=true)](https://ci.appveyor.com/project/xleon/reliablesignalrclient)

## ReliableSignalRClient

Fluent SignalR client with support for disconnected state

Method invokes may be lost at some point due to connection failure or other reasons. 
Forget about try/catch statements when using SignalR. 
QueuedSignalRClient will remember those calls along with the passed arguments and will retry to send them to the server once connection is restored. 
I did this utility when working on a "whatsapp" like style chat, ensuring every message gets delivered.

### TODO

- Plugable Cache interface
- Documentation
- Samples
- Nuget package
- Tests