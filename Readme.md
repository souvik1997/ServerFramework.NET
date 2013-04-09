# ServerFramework.NET


ServerFramework.NET is an __asynchronous__, __robust__, __open-source__, __extensible__, and __easy to use__ TCP server framework written in C# for .NET 2.0 and higher.

### Features:

  - Completely asynchronous and handles multiple clients at once
  - Simple and documented code
  - Easy to get running and comes with an example
  - Arbitrary identification tags for clients

## Changelog

v1.0: Initial version

## Installation

Simply add ServerFramework.NET.dll as a reference in your .NET project and add the following line of code to the beginning of your source file.

```C#
using ServerFramework.NET;
```

## Usage

Instantiate the Server with one of the four constructors:

```C#
var server = new Server(int port, int dataSize)
```

```C#
var server = new Server(int port, int dataSize, int maxNumOfClients)
```

```C#
var server = new Server(int port, List<char> messageDelimiters)
```    

```C#
var server = new Server(int port, List<char> messageDelimiters, int maxNumOfClients)
```

Add event handlers:

```C#
server.OnMessageReceived += server_OnMessageReceived; 
server.OnServerStop += (o, i) => 
  {
      Console.WriteLine("Server stopped");
      Application.Exit();
  };
```

```C#
static void server_OnMessageReceived(object sender, ClientEventArgs e)
{
  var client = e.Client;
  string msg = Encoding.ASCII.GetString(client.Message);
  Console.WriteLine(msg);
  client.SendData("I heard you! " + msg + "\n");
}
```

And start the server!

```C#
server.StartAsync();
```


License
-------

This library and all associated demonstration and example programs are released under the GNU LGPL version 3. You can find a copy of this license in the file __LICENSE.txt__.
If this file did not come with this software, the license is also [online at http://gnu.org](http://www.gnu.org/licenses/lgpl.html).

If you use this library, feel free to *Star* this repository!
  
Copyright (c) Souvik Banerjee  2013
    