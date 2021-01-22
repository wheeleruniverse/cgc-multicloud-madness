
const url = "https://v21oicmkdc.execute-api.us-east-1.amazonaws.com";

function getAmazonPresignedUrl(success, failure, args){
    
    $.ajax({
        url: `${url}/getAmazonPresignedUrl`,
        type: "GET",
        success: function(data){ 
            success(data, args);
        },
        error: function(xhr, status, error){
            console.error(`getAmazonPresignedUrl: ERROR: ${error}`);
            
            if (failure){
                failure(xhr, status, error);
            }
        }
    });
}


function queryAzureAnalysisTable(success, failure, args){
    
    $.ajax({
        url: `${url}/queryAzureAnalysisTable`,
        type: "GET",
        beforeSend: function(xhr){
            xhr.setRequestHeader('Accept', 'application/json;odata=nometadata');
        },
        success: function(data) { 
            success(data, args);
        },
        error: function(xhr, status, error) {
            console.error(`queryAzureAnalysisTable: ERROR: ${error}`);
            if (failure){
                failure(xhr, status, error);
            }
        }
    });
}