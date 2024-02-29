var connectionUserCount = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();

connectionUserCount.on("ReceiveMessage", (user, message) => {

    var sender = document.getElementById("senderMessage")
    sender.innerText = user.toString();

    var msg = document.getElementById("Message")
    msg.innerText = message.toString();

   // var imgElement = document.getElementById("imageElement");
   // imgElement.src = "data:image/bmp;base64," + base64String;

})


connectionUserCount.on("ApiLink", (ApiLink) => {

    var apiLink = document.getElementById("apiLink")
    apiLink.innerText = ApiLink.toString();  


    // Fetch the API endpoint
fetch(ApiLink.toString())
.then(response => response.text()) // Assuming the response is a text (base64 string)
.then(base64String => {
 
   var imgElement = document.getElementById("imageElement");
    imgElement.src = "data:image/bmp;base64," + base64String;


})
.catch(error => {
  console.error('Error fetching API:', error);
});





})







function newWindowLoadedOnClient() {
    connectionUserCount.send("SendApiLink");
}


//start connection
function fulfilled() {
    console.log("Connection to user hub is successful")
    newWindowLoadedOnClient()
}

function rejected() {
    console.log("Connection to user hub rejected")
}


connectionUserCount.start().then(fulfilled, rejected)



