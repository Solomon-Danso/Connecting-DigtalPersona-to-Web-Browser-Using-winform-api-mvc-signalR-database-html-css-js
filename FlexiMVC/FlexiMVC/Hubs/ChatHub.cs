using Microsoft.AspNetCore.SignalR;

namespace FlexiMVC.Hubs
{
    public class ChatHub:Hub
    {
          private static List<string> TotalUsers = new List<string>(); 


        public async Task SendMessageMainFunction(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

         public async Task SendMessageToConnectionId(string connectionId, string user, string message)
        {
           // await Clients.Client(connectionId).SendAsync("ReceiveMessage", user, message,base64String);
            await Clients.Client(connectionId).SendAsync("ReceiveMessage", user, message);
        }

        public async Task SendApiLink(string connectionId, string ApiLink){

            await Clients.Client(connectionId).SendAsync("ApiLink", ApiLink);
        }





        public override Task OnConnectedAsync()
        {
            
            string connectionId = Context.ConnectionId;
            TotalUsers.Add(connectionId); // Add connection ID to the list
            Clients.All.SendAsync("ConnectionList", TotalUsers);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            string connectionId = Context.ConnectionId;
            TotalUsers.Remove(connectionId); // Remove connection ID from the list
            Clients.All.SendAsync("ConnectionList", TotalUsers);
            return base.OnDisconnectedAsync(exception);
        }






    }
}
