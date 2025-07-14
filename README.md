This GitHub repository, `cgc-multicloud-madness`, is part of the "#CloudGuruChallenge: Multi-Cloud Madness," focused on building a serverless image processing pipeline across multiple cloud providers. The project demonstrates a practical application of multi-cloud architecture, leveraging the strengths of different cloud services for specific tasks.

### Project Overview

The core objective of this project, as part of the Cloud Guru Challenge, is to architect and implement an image upload and recognition system utilizing at least three distinct cloud providers. This solution provides a simple web interface for users to upload images, which are then processed through a serverless pipeline that includes image storage, recognition, and metadata management in a NoSQL database.

### Architecture

The author's approach to the multi-cloud architecture involves:

* **Cloud Provider 1 (AWS):** Used for the serverless website and compute services. This typically involves services like AWS S3 for storage and AWS Lambda for serverless functions, deployed using the Serverless Application Model (SAM).
* **Cloud Provider 2 (GCP - Google Cloud Platform):** Employed for the machine learning image processing service, specifically GCP Cloud Vision. This service is used to analyze uploaded images, including its Safe Search feature for content filtering.
* **Cloud Provider 3 (Azure):** Utilized for the NoSQL database, specifically Azure Tables Storage. This was chosen for its simplicity and cost-effectiveness in storing image metadata and URLs. The database design incorporates a randomly generated PartitionKey for optimized queries.

### Technologies Used

The repository primarily features:

* **C# (48.4%):** Likely used for backend logic and serverless functions.
* **JavaScript (33.9%):** For frontend interactivity of the web application.
* **HTML (9.0%):** For the structure of the web interface.
* **CSS (8.7%):** For styling the web application.
* **AWS Serverless Application Model (SAM):** For deploying serverless components on AWS.
* **Azure Tables Storage:** NoSQL database for metadata.
* **Google Cloud Vision API:** For image recognition and safe search.

### Challenge Requirements Met

This project addresses the key requirements of the "Multi-Cloud Madness" challenge by:

* Creating a simple web page for image uploads.
* Saving pictures to a storage service on Cloud Provider 1 (AWS S3).
* Triggering a serverless process to call an Image Recognition service on Cloud Provider 2 (GCP Cloud Vision).
* Storing metadata and the image URL in a NoSQL database on Cloud Provider 3 (Azure Tables Storage).
* Utilizing fully managed services from the respective cloud providers.

### Repository Structure

The repository includes notable directories such as:

* `csharp`: Likely contains the C# source code for serverless functions or other backend components.
* `sam`: Contains Serverless Application Model templates for AWS deployments.
* `web`: Holds the static web assets (HTML, CSS, JavaScript) for the frontend.

### Additional Resources

For more details on the project and the challenge, refer to the following resources:

* **Cloud Guru Challenge - Multi-Cloud Madness:** [Cloud Guru Challenge Blog](https://www.pluralsight.com/resources/blog/cloud/cloudguruchallenge-multi-cloud-madness)
* **Author's Blog Post:** [Multi Cloud Madness Blog Post](https://dev.to/wheeleruniverse/january-21-cloudguruchallenge-iaj)
* **GitHub Repository:** [cgc-multicloud-madness](https://github.com/wheeleruniverse/cgc-multicloud-madness)
