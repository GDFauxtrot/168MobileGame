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
        }

        return messageContents;
    }
}
