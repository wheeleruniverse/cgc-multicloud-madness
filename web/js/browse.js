(
function() {

    function startup() {
        
        queryAzureAnalysisTable(getData);
    }
    
    function getData(data){
        
        $.each(data['Data'], function(idx, r){
            
            var labels = JSON.parse(r.SerializedVisionAnalysis).Labels;
            if (isUnsafe(labels)){
                return true; // continue
            }
            
            var recordId = r.RowKey;
            var labelsId = `${recordId}-labels`;
            
            var recordSelector = `#${recordId}`;
            var labelsSelector = `#${labelsId}`;
            
            
            $("<div/>", {
                "id": recordId,
                "class": "data-record"
            })
            .appendTo("#content");
            
            
            getAmazonPresignedGet(r.S3BucketName, r.S3ObjectName, function(data){
                $("<img/>", {
                    "data-key": r.RowKey,
                    "data-partition": r.PartitionKey,
                    "data-s3bucket": r.S3BucketName,
                    "data-s3object": r.S3ObjectName,
                    "data-timestamp": r.Timestamp,
                    "src": data['Url']
                })
                .appendTo(recordSelector);
                
                $("<div/>", {
                    "id": labelsId,
                    "class": "data-labels"
                })
                .appendTo(recordSelector);
                
                $.each(labels, function(jdx, v){
                    
                    var fieldId = `${labelsId}-${jdx}`;
                    var fieldSelector = `#${fieldId}`;
                    
                    $("<div/>", {
                        "id": fieldId,
                        "class": "label"
                    })
                    .appendTo(labelsSelector);
                    
                    $("<span/>", {
                        "html": `Label.Name: ${v.Name}`
                    })
                    .appendTo(fieldSelector);
                    
                    $("<span/>", {
                        "html": `Label.Likelihood: ${v.Likelihood}`
                    })
                    .appendTo(fieldSelector);
                    
                    $("<span/>", {
                        "html": `Label.Score: ${v.Score}`
                    })
                    .appendTo(fieldSelector);
                });
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