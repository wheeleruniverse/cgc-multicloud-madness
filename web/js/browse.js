$(document).ready(function () {
    
    var azureAnalysisTableData = {};
    
    $("#modal-close").click(function() {
        $(".modal-data").remove();
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
                    "click": clickImage
                })
                .appendTo("#content");
            });
        });
    });
    
    
    function clickImage(){
        
        var data = azureAnalysisTableData[$(this).attr("data-key")];
        var date = data.Timestamp;
        
        $("#modal-content,#modal-background").toggleClass("active");
                
        $("<div/>", {
            "class": "modal-data",
            "html": `
                <div id="modal-data-wrapper">
                    <p id="img-name">${data.S3ObjectName} // ${date.substr(0, date.indexOf('T'))} @ ${date.substr(date.indexOf('T') + 1, 8)} UTC</p>
                    <br/>
                    <img class="modal-data" src=${$(this).attr("src")} />
                    <hr/>
                    ${convertLabelsToHtml(JSON.parse(data.SerializedVisionAnalysis).Labels)}
                </div>
            `
        })
        .appendTo("#modal-content");
    }
    
    function convertLabelsToHtml(labels){
        
        var adultNames = ["Adult", "Medical", "Nsfw", "Racy", "Spoof", "Violence"];
        var adultTable = "<table><tr><th>Label</th><th>Likelihood</th></tr>";
        var labelTable = "<table><tr><th>Label</th><th>Score</th></tr>";
        
        $.each(labels, function(idx, r){
            
            if(adultNames.includes(r.Name)){
                adultTable += `<tr><td>${r.Name}</td><td>${convertLikelihoodToString(r.Likelihood)}</td></tr>`
            }
            else {
                labelTable += `<tr><td>${r.Name}</td><td>${r.Score}</td></tr>`
            }
        });
        
        adultTable += "</table>";
        labelTable += "</table>";
        return `<div>${adultTable}${labelTable}</div>`;
    }
    
    
    function convertLikelihoodToString(likelihood){
        
        switch(likelihood) {
          case 1:
            return "Very Unlikely"
          case 2:
            return "Unlikely"
          case 3:
            return "Possible"
          case 4:
            return "Likely"
          case 5:
            return "Very Likely"
          default:
            return "Unknown"
        }
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
});