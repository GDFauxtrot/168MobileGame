using System.Collections;
using System.Collections.Generic;
public static class MessageParser {

    public static List<object> ParseMessage(string message) {
        List<object> messageContents = new List<object>();

        if (message == "")
            return null;

        string[] messages = message.Split(':');
        
        if (messages[0] == "j") {
            messageContents.Add(messages[0]);
            messageContents.Add(bool.Parse(messages[1]));
            string[] pos = messages[2].Split(',');
            messageContents.Add(float.Parse(pos[0]));
            messageContents.Add(float.Parse(pos[1]));
            messageContents.Add(float.Parse(pos[2]));
        }
        if (messages[0] == "T") {
            messageContents.Add(messages[0]);
            messageContents.Add(float.Parse(messages[1]));
        }
        if (messages[0] == "b") {
            messageContents.Add(messages[0]);
            string[] pos = messages[1].Split(',');
            messageContents.Add(float.Parse(pos[0]));
            messageContents.Add(float.Parse(pos[1]));
            messageContents.Add(float.Parse(pos[2]));
        }
        return messageContents;
    }
}
