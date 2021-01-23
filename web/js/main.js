
const root = "https://v21oicmkdc.execute-api.us-east-1.amazonaws.com";

function getAmazonPresignedGet(bucketName, objectName, success, failure, args){
    
    let url = bucketName && objectName 
        ? `${root}/getAmazonPresignedUrl?bucketName=${bucketName}&objectName=${objectName}` 
        : `${root}/getAmazonPresignedUrl`;
    
    $.ajax({
        url: url,
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


function getAmazonPresignedPut(success, failure, args){
    return getAmazonPresignedGet(null, null, success, failure, args);
}


function queryAzureAnalysisTable(success, failure, args){
    
    $.ajax({
        url: `${root}/queryAzureAnalysisTable`,
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