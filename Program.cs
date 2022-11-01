using ConsoleApp1.Core;
using ConsoleApp1.GameElements;

WebsocketServer wss = new WebsocketServer("127.0.0.1", 80);

Npc npc = new Npc();

npc.mindset.opinions.Add("player", new PonderatedRandomList<NpcMindset.OpinionType>());
npc.mindset.opinions["player"].Add(NpcMindset.OpinionType.Neutral, 10);

for (int i = 0; i < 1000; i++)
{
    var key = Console.ReadKey();
    Console.Clear();

    // If key pressed is +, add a positive opinion
    if (key.Key == ConsoleKey.Add)
    {
        npc.mindset.AddOpinion("player", NpcMindset.OpinionType.YES);
    }
    // If key pressed is -, add a negative opinion
    else if (key.Key == ConsoleKey.Subtract)
    {
        npc.mindset.AddOpinion("player", NpcMindset.OpinionType.NO);
    }

    // Get the opinion of the player
    npc.mindset.opinions["player"].show_probabilities();

    string message = "RenderNpcMindset:player;";
    foreach (var e in npc.mindset.opinions["player"]._list) message += e.Key + ",";
    message.Remove(message.Length - 1, 1);
    message += ";";
    foreach (var e in npc.mindset.opinions["player"]._list) message += e.Value + ",";

    WebSocketClient.clients[0].Send(message);

}


