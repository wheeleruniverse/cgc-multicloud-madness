'use strict'

const aws = require('aws-sdk')
aws.config.update({ region: process.env.AWS_REGION })

const s3 = new aws.S3({signatureVersion: 'v4'})

// AWS Lambda entry point
exports.handler = async (event) => {
    return await getSignedUrl(event)
}

const getSignedUrl = async function(event) {
    
    // query parameters
    let queryParameters = event.queryStringParameters;
    let bucketName = queryParameters ? queryParameters["bucketName"] : null;
    let objectName = queryParameters ? queryParameters["objectName"] : null;
    
    let methodName;
    let objectType = null;
    
    if (bucketName && objectName){
        methodName = "getObject";
    }
    else {
        bucketName = process.env.rPictureBucket;
        methodName = "putObject";
        objectName = `${parseInt(Math.random() * 10000000)}.png`
        objectType = "image/png";
    }
    
    let url = await s3.getSignedUrlPromise(methodName, {
        Bucket: bucketName,
        ...(objectType && {ContentType: objectType}),
        Expires: 90,
        Key: objectName
    });
    
    return {
        BucketName: bucketName,
        MethodName: methodName,
        ObjectName: objectName,
        ObjectType: objectType,
        Url: url
    }
}