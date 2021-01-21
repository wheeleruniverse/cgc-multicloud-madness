(
function() {

    function startup() {
        
        queryAzureAnalysisTable(drawResults);
        
        /*
        var analysisUrl = [];
        analysisUrl.push(

        );
        
        $.ajax({
            url: analysisUrl.join(""),
            type: "GET",
            beforeSend: function(xhr){
                xhr.setRequestHeader('Accept', 'application/json;odata=nometadata');
            },
            success: function(data) { 
                drawResults(data.value);
            },
            error: function (xhr, status, error) {
                console.log("error: " + error);
            }
        });
        */
    }
    
    
    function drawResults(data){
        
        console.log(`Partition: ${data['Partition']}`);
        
        $.each(data['Data'], function(idx, r){
            
            var labels = JSON.parse(r.SerializedVisionAnalysis).Labels;
            if(isUnsafe(labels)){
                return true; // continue
            }
            
            var key = r.PartitionKey + "_" + r.RowKey;
            $("<div/>", {
                "id": key,
                "class": "data-record"
            })
            .appendTo("#content");
                        
            $("<span/>", {"html": "Index: " + idx + "<br/>"}).appendTo("#" + key);
            $("<span/>", {"html": "PartitionKey: " + r.PartitionKey + "<br/>"}).appendTo("#" + key);
            $("<span/>", {"html": "RowKey: " + r.RowKey + "<br/>"}).appendTo("#" + key);
            $("<span/>", {"html": "S3BucketName: " + r.S3BucketName + "<br/>"}).appendTo("#" + key);
            $("<span/>", {"html": "S3ObjectName: " + r.S3ObjectName + "<br/>"}).appendTo("#" + key);
            $("<span/>", {"html": "Timestamp: " + r.Timestamp + "<br/>"}).appendTo("#" + key);
            $("<br/>").appendTo("#" + key);
            
            $.each(labels, function(jdx, v){
                $("<span/>", {"html": "Label.Name: " + v.Name + "<br/>"}).appendTo("#" + key);
                $("<span/>", {"html": "Label.Likelihood: " + v.Likelihood + "<br/>"}).appendTo("#" + key);
                $("<span/>", {"html": "Label.Score: " + v.Score + "<br/>"}).appendTo("#" + key);
                $("<br/>").appendTo("#" + key);
            });
        });
    }
    
    
    function isUnsafe(labels){
        
        var unsafe = false;
        $.each(labels, function(idx, r){
            if("Nsfw" === r.Name && r.Likelihood > 2){
                console.log("Potentially Unsafe Content");
                unsafe = true;
                return false; // break
            }
        });
        return unsafe;
    }
    
    
    window.addEventListener('load', startup, false);
    
})();