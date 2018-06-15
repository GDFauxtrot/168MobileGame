using System.Collections;
using System.Collections.Generic;
public static class MessageParser {

    public static List<object> ParseMessage(string message) {
        List<object> messageContents = new List<object>();

        if (message == "")
            return null;

        string[] messages = message.Split(':');
        
        if (messages[0] == "jump") {
            messageContents.Add(messages[0]);
            messageContents.Add(bool.Parse(messages[1]));
            string[] pos = messages[2].Split(',');
            messageContents.Add(float.Parse(pos[0]));
            messageContents.Add(float.Parse(pos[1]));
            messageContents.Add(float.Parse(pos[2]));
        }
        if (messages[0] == "start") {
            messageContents.Add(messages[0]);
            messageContents.Add(System.TimeSpan.Parse(messages[1]));
        }
        if (messages[0] == "block") {
            messageContents.Add(messages[0]);
            string[] pos = messages[1].Split(',');
            messageContents.Add(float.Parse(pos[0]));
            messageContents.Add(float.Parse(pos[1]));
            messageContents.Add(float.Parse(pos[2]));
        }
        if (messages[0] == "pos") {
            messageContents.Add(messages[0]);
            messageContents.Add(System.DateTime.Parse(messages[1]));
            string[] pos = messages[2].Split(',');
            messageContents.Add(float.Parse(pos[0]));
            messageContents.Add(float.Parse(pos[1]));
            messageContents.Add(float.Parse(pos[2]));
        }
        if (messages[0] == "ded") {
            messageContents.Add(messages[0]);
        }
        return messageContents;
    }
}
