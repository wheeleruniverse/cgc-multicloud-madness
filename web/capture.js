(
function() {

    // image dimensions
    var height = 480;
    var width = 640;    
         
    // document elements
    var button = null;
    var canvas = null;
    var video = null;
    
    // local state
    var streaming = false;


    function startup() {
        
        // find elements
        button = document.getElementById('start-btn');
        canvas = document.getElementById('canvas');
        video = document.getElementById('video');

        // stream user media
        navigator.mediaDevices.getUserMedia({video: true, audio: false}).then(function(stream) {
            video.srcObject = stream;
            video.play();
        })
        .catch(function(err) {
            console.log("ERROR: " + err);
        });


        // set image dimensions
        video.addEventListener('canplay', function(event){
            
            if (!streaming) {
                canvas.setAttribute('width', width);
                canvas.setAttribute('height', height);
                video.setAttribute('width', width);
                video.setAttribute('height', height);
                streaming = true;
            }
            
        }, false);


        // configure click handler to save the picture
        button.addEventListener('click', function(event){
            savePicture();
            event.preventDefault();
            
        }, false);
    }


    function savePicture() {
        
        canvas.height = height;
        canvas.width = width;
        
        var context = canvas.getContext('2d');
        context.drawImage(video, 0, 0, width, height);
        
        
        $.get('https://8rhqieqz48.execute-api.us-east-1.amazonaws.com/auth', function(data, status){
            
            if ("success" === status){
                
                canvas.toBlob(function(b) {
                    
                    if(b === null){
                        console.log("ERROR: Blob is null");
                        return;
                    }
                    $.ajax({
                        url: data['uploadURL'],
                        type: 'PUT',
                        contentType: 'image/png',  
                        data: b,
                        processData: false
                    });
                    
                });                
            }
            else {
                console.log("ERROR: " + status);
            }
        });
    }

    window.addEventListener('load', startup, false);

})();