$(document).ready(function () {
    
    var azureAnalysisTableData = {};
    
    $("#modal-close").click(function() {
        $("#modal-data").remove();
        $("#modal-content,#modal-background").toggleClass("active");
    });
    
    
    queryAzureAnalysisTable(function(data){
        
        // filter unsafe records
        var records = $.grep(data['Data'], function(r){
            var labels = JSON.parse(r.SerializedVisionAnalysis).Labels;
            return !isUnsafe(labels);
        }); 
        console.log(`Loaded ${records.length} "Safe" Record(s)`);
        
        // populate records
        $.each(records, function(idx, r){
            
            azureAnalysisTableData[r.RowKey] = r;
            
            getAmazonPresignedGet(r.S3BucketName, r.S3ObjectName, function(auth){
                
                $("<img/>", {
                    "data-key": r.RowKey,
                    "src": auth['Url'],
                    "click": function(){
                        
                        $("#modal-content,#modal-background").toggleClass("active");
                        
                        var key = $(this).attr("data-key");
                        
                        $("<div/>", {
                            "id": "modal-data",
                            "html": JSON.stringify(azureAnalysisTableData[key])
                        })
                        .appendTo("#modal-content");
                    }
                })
                .appendTo("#content");
            });
        });
    });
    
    
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

                           
/*
TODO:

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
*/
    

    
    
    
    
});