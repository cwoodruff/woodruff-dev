---
title: "Terraform + Azure DevOps: Automate Your Cloud Deployments the Smart Way"
date: 2025-02-26
categories: 
  - "terraform"
tags: 
  - "azure"
  - "devops"
  - "iac"
  - "infrastructure"
  - "terraform"
coverImage: "./images/Terraform-Azure-DevOps.png"
---

Manually deploying infrastructure was the last decade**.** If you’re still running `terraform apply` on your local machine, it’s time to **step up your game** with **Azure DevOps Pipelines**!

In this post, we’ll cover how to:  
**Automate Terraform deployments** using Azure DevOps  
**Set up an Azure DevOps pipeline** for Terraform  
**Store Terraform state securely** in Azure  
**Integrate Terraform into CI/CD workflows**

Let’s build an automated, **version-controlled, and scalable** Terraform pipeline!

* * *

## **1\. Why Use Terraform with Azure DevOps?**

If you’re managing infrastructure manually, you risk:  
**Inconsistent deployments** – Human error is real!  
**State conflicts** – Running Terraform from different machines can cause chaos.  
**No audit trail** – Who deployed what, and when?

By integrating **Terraform with Azure DevOps**, you get:  
**Automated infrastructure changes** with CI/CD.  
**Version-controlled deployments** using Git.  
**Secure, shared Terraform state** across teams.

* * *

## **2\. Setting Up Terraform in Azure DevOps**

Before we create a pipeline, we need a **Terraform backend** to store our state. **Azure Storage is perfect for this**!

### **Step 1: Create an Azure Storage Account for Terraform State**

Run the following commands to set up **Azure Blob Storage** for storing Terraform state:

```bash
az group create --name myTerraformRG --location eastus
az storage account create --name mytfstorage --resource-group myTerraformRG --location eastus --sku Standard_LRS
az storage container create --name tfstate --account-name mytfstorage
```

Now, configure **Terraform’s backend** to use this storage:

```
terraform {
  backend "azurerm" {
    resource_group_name  = "myTerraformRG"
    storage_account_name = "mytfstorage"
    container_name       = "tfstate"
    key                 = "terraform.tfstate"
  }
}
```

**Why It’s Important:** This ensures **state consistency** and allows multiple team members to use Terraform safely.

* * *

## **3\. Creating an Azure DevOps Pipeline for Terraform**

Azure DevOps **pipelines** let you run Terraform automatically every time you push a change to Git. Let’s create one!

### **Step 1: Set Up Azure DevOps**

1. **Create a new repository** in Azure DevOps for your Terraform code.

3. **Enable Pipelines** in your project.

5. **Store Terraform configuration** (`main.tf`, `variables.tf`, etc.) in the repo.

### **Step 2: Define the Azure DevOps Pipeline (`azure-pipelines.yml`)**

Create a **pipeline file** (`azure-pipelines.yml`) in the root of your repo:

```yaml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: TerraformInstaller@0
    displayName: "Install Terraform"
    inputs:
      terraformVersion: '1.5.0'

  - script: terraform init
    displayName: "Initialize Terraform"

  - script: terraform plan -out=tfplan
    displayName: "Terraform Plan"

  - script: terraform apply -auto-approve tfplan
    displayName: "Terraform Apply"
```

### **Breaking It Down:**

**Installs Terraform** on the Azure DevOps agent.  
**Initializes Terraform** (`terraform init`).  
**Runs Terraform Plan** (`terraform plan`) to preview changes.  
**Applies changes** (`terraform apply`) automatically.

**Now, every commit to `main` triggers Terraform!**

* * *

## **4\. Using Terraform Variables in Azure DevOps**

Instead of hardcoding values, **use Azure DevOps variables** for dynamic configurations!

### **Step 1: Define Variables in Azure DevOps**

1\. Go to **Pipelines** → **Library** → **Add a Variable Group**.  
2\. Add variables like:

- `TF_VAR_location = eastus`

- `TF_VAR_environment = dev`

### **Step 2: Reference Variables in Terraform (`variables.tf`)**

```
variable "location" {}
variable "environment" {}
```

### **Step 3: Modify the Pipeline to Use Variables**

```yaml
steps:
  - script: terraform apply -var="location=$(TF_VAR_location)" -auto-approve
    displayName: "Apply Terraform with Variables"
```

**Why It’s Cool:** Now you can deploy to **different environments** without changing Terraform code!

* * *

## **5\. Adding Terraform Security Checks to the Pipeline**

Infrastructure security **shouldn’t be an afterthought**! Add security scans to Terraform pipelines using **tfsec** and **Checkov**.

### **Step 1: Add `tfsec` to Your Pipeline**

Modify `azure-pipelines.yml` to include a **security scan** before applying Terraform:

```yaml
- script: |
    curl -s https://raw.githubusercontent.com/aquasecurity/tfsec/master/scripts/install_linux.sh | bash
    tfsec .
  displayName: "Run tfsec Security Scan"
```

**Now, Terraform will fail if there are security issues!**

* * *

## **6\. Using Terraform Workspaces for Multi-Environment Deployments**

Instead of maintaining **separate Terraform configurations** for dev, staging, and prod, **use Terraform workspaces**.

### **Step 1: Add Workspaces to Your Pipeline**

Modify `azure-pipelines.yml` to dynamically switch workspaces:

```yaml
- script: terraform workspace select $(TF_VAR_environment) || terraform workspace new $(TF_VAR_environment)
  displayName: "Select or Create Terraform Workspace"
```

**Now, one pipeline manages multiple environments!**

* * *

## **7\. Best Practices for Terraform in Azure DevOps**

**Use remote state** – Store Terraform state in Azure Blob Storage.  
**Secure credentials** – Use **Azure Key Vault** for sensitive secrets.  
**Implement approval gates** – Require manual approval before applying changes.  
**Run security scans** – Use **tfsec** and **Checkov** in pipelines.  
**Use workspaces** – Manage multiple environments without duplication.

* * *

## **Wrapping Up**

By integrating **Terraform with Azure DevOps**, you can:

- **Automate cloud deployments** with CI/CD.

- **Maintain a secure, version-controlled Terraform workflow**.

- **Deploy infrastructure safely to multiple environments**.

Now, go build something **awesome and automated!**

* * *

### **What’s Next?**

Terraform automation is great, but **what about cloud cost optimization?** In the next post, **"Cost Management with Terraform,"** we’ll explore how to **track, analyze, and reduce cloud costs** using Terraform’s built-in features and third-party tools—so you can save money while keeping your infrastructure scalable.
