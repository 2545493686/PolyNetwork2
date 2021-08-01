using GameGravity.PolyNetwork;
using System;
using System.Threading;

// 服务端示例
var server = new PolyNetwork { Logger = Console.WriteLine, Name = "Server" }
    .Listen("127.0.0.1", 2333)
    .AddListener(msg => new { text = $"hi, {msg.id}." });


// 客户端示例
var client = new PolyNetwork { Logger = Console.WriteLine, Name = "Client" };

var connectReport = await client.Connect("127.0.0.1", 2333);
if (connectReport == ConnectResult.error) return;

//Thread.Sleep(10000);

for (int i = 0; i < 100; i++)
{
    Console.WriteLine("======== 客户端发送消息：hello ========");
    
    var serverReport = await client.Send(new { id = $"{i}", text = "hello" });
    
    if (serverReport == null) break;

    Console.WriteLine($"======== 服务端返回消息：{serverReport.text} ========");
}
