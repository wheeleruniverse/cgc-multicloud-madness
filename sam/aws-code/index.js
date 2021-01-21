'use strict'

const aws = require('aws-sdk')
aws.config.update({ region: process.env.AWS_REGION })

const s3 = new aws.S3({signatureVersion: 'v4'})

// AWS Lambda entry point
exports.handler = async (event) => {
    return await getSignedUrl(event)
}

const getSignedUrl = async function(event) {
    
    const randomId = parseInt(Math.random() * 10000000)
    const objectId = `${randomId}.png`
    const params = {
        Bucket: process.env.rPictureBucket,
        Key: objectId,
        Expires: 300,
        ContentType: 'image/png'
    }
    console.log(`Params: ${params}`)
    
    const url = await s3.getSignedUrlPromise('putObject', params);
    return JSON.stringify({
        Key: objectId,
        Url: url
    })
}