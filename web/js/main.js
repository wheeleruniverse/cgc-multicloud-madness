
const url = "https://v21oicmkdc.execute-api.us-east-1.amazonaws.com";

function getAmazonPresignedUrl(success, failure){
    
    $.ajax({
        url: `${url}/getAmazonPresignedUrl`,
        type: "GET",
        success: function(data) { 
            success(data);
        },
        error: function (xhr, status, error) {
            console.error(`getAmazonPresignedUrl: ERROR: ${error}`);
            failure(xhr, status, error);
        }
    });
}


function queryAzureAnalysisTable(success, failure){
    
    $.ajax({
        url: `${url}/queryAzureAnalysisTable`,
        type: "GET",
        beforeSend: function(xhr){
            xhr.setRequestHeader('Accept', 'application/json;odata=nometadata');
        },
        success: function(data) { 
            success(data);
        },
        error: function(xhr, status, error) {
            console.error(`queryAzureAnalysisTable: ERROR: ${error}`);
            failure(xhr, status, error);
        }
    });
}