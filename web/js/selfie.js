(
function() {

    // image dimensions
    var height = 480;
    var width = 640;    
         
    // document elements
    var alert = null;
    var button = null;
    var canvas = null;
    var video = null;
    
    // local state
    var streaming = false;


    function startup() {
                
        // find elements
        alert = document.getElementById('alert');
        button = document.getElementById('start-btn');
        canvas = document.getElementById('canvas');
        video = document.getElementById('video');

        // stream user media
        navigator.mediaDevices.getUserMedia({video: true, audio: false}).then(function(stream) {
            video.srcObject = stream;
            video.play();
            button.disabled = false;
        })
        .catch(function(err) {
            updateAlert(err, false);
        });


        // set image dimensions
        video.addEventListener('click', function(event){
            
            if (!streaming) {
                canvas.setAttribute('width', width);
                canvas.setAttribute('height', height);
                video.setAttribute('width', width);
                video.setAttribute('height', height);
                streaming = true;
            }
            
        }, false);
        
        video.click();


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
        
        
        getAmazonPresignedPut(
        
            // success
            function(data){
                canvas.toBlob(function(b) {
                    
                    if(b === null){
                        updateAlert('image blob is invalid', false);
                        return;
                    }
                    $.ajax({
                        url: data['Url'],
                        type: 'PUT',
                        contentType: 'image/png',  
                        data: b,
                        processData: false
                    });
                    
                    updateAlert('image captured', true);
                });
            },
            
            // failure
            function(xhr, status, error){
                updateAlert('authentication api failed', false);
            }
        );
    }
    
    
    function updateAlert(message, isSuccess){
        
        var alertType = isSuccess ? "success" : "failure";
        var alertText = alertType + ":" + message;
        
        if(!isSuccess){
            console.log(alertText);
        }
        
        alert.classList.remove('alert-danger');
        alert.classList.remove('alert-success');
        
        alert.classList.add(isSuccess ? 'alert-success' : 'alert-danger');
        alert.innerHTML = alertText;
    }

    window.addEventListener('load', startup, false);

})();