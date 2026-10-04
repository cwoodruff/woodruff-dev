---
title: "Terraform Variables &amp; Outputs: The Secret Sauce of Reusable Infrastructure"
date: 2025-02-20
categories: 
  - "terraform"
tags: 
  - "devops"
  - "iac"
  - "infrastructure"
  - "terraform"
coverImage: "./images/Terraform-Variables-Outputs-The-Secret-Sauce-of-Reusable-Infrastructure.png"
---

If you've ever hardcoded values in Terraform, I have some bad news… and some great news! The **bad news**? Hardcoding is a one-way ticket to frustration and messy code. The **great news**? Terraform variables and outputs can **save your sanity** by making your infrastructure code **dynamic, reusable, and scalable**.

In this post, we’ll break down **how to use Terraform variables** like a pro and how **outputs** can help you retrieve useful information from your deployments. Buckle up!

* * *

## **Why Should You Use Variables?**

Imagine you’re deploying infrastructure for **multiple environments** (dev, staging, prod). If you **hardcode values**, you’ll have to maintain **separate Terraform files** for each environment—yikes!

**Terraform variables fix this by:**

1. Making configurations **reusable** across multiple environments.

3. **Reducing code duplication** and maintenance headaches.

5. Allowing **easy customization** via command-line, environment variables, or files.

* * *

## **Types of Variables in Terraform**

Terraform supports **three types of variables**, and each serves a unique purpose:

| **Variable Type** | **What It Stores** | **Example** |
| --- | --- | --- |
| **String** | Text values | `"eastus"` |
| **Number** | Numeric values | `2` |
| **Boolean** | True/False values | `true` |
| **List** | A collection of values | `["dev", "staging", "prod"]` |
| **Map** | Key-value pairs | `{ env = "prod", region = "us-west" }` |

* * *

## **Declaring and Using Variables**

Terraform variables are **declared** in a `variables.tf` file. Here’s how you define a **string variable** for an Azure region:

```
variable "location" {
  description = "The Azure region for deployment"
  type        = string
  default     = "East US"
}
```

Now, instead of hardcoding the location, reference it like this in `main.tf`:

```
resource "azurerm_resource_group" "example" {
  name     = "myResourceGroup"
  location = var.location
}
```

### **Passing Variables in Terraform**

You can set variable values in **multiple ways**:

1. **Command-Line Flags**bashCopyEdit`terraform apply -var="location=West US"`

3. **Environment Variables**bashCopyEdit`export TF_VAR_location="West US"`

5. **Variable Files (`.tfvars`)**  
    Create a `terraform.tfvars` file:hclCopyEdit`location = "West US"` Then apply it:bashCopyEdit`terraform apply -var-file="terraform.tfvars"`

**Pro Tip:** Use `.tfvars` files for different environments (`dev.tfvars`, `prod.tfvars`) to easily switch configurations.

* * *

## **Using Lists and Maps**

Want to make your Terraform configs even more **powerful**? Use **lists** and **maps** to store multiple values.

#### **Example: Using a List**

A list allows multiple values to be stored dynamically.

```
variable "allowed_locations" {
  description = "List of approved Azure regions"
  type        = list(string)
  default     = ["East US", "West US", "Central US"]
}
```

To use a list value:

```
resource "azurerm_resource_group" "example" {
  name     = "myResourceGroup"
  location = var.allowed_locations[0] # Selects "East US"
}
```

#### **Example: Using a Map**

A map stores key-value pairs for better organization.

```
variable "environment_configs" {
  description = "Settings per environment"
  type        = map(string)
  default = {
    dev     = "Standard_DS1_v2"
    staging = "Standard_DS2_v2"
    prod    = "Standard_DS3_v2"
  }
}
```

To retrieve values dynamically:

```
resource "azurerm_virtual_machine" "example" {
  name     = "myVM"
  vm_size  = var.environment_configs["prod"] # "Standard_DS3_v2"
}
```

* * *

## **Outputs: Getting Useful Info from Terraform**

Terraform **outputs** let you **retrieve key details** from your infrastructure after deployment—like the name of a resource group or the public IP of a VM.

### **Defining Outputs**

Outputs are declared in an `outputs.tf` file like this:

```
output "resource_group_name" {
  value = azurerm_resource_group.example.name
}
```

### **Running Terraform Apply**

After running `terraform apply`, Terraform displays the outputs:

```bash
Apply complete! Resources: 1 added.

Outputs:

resource_group_name = "myResourceGroup"
```

Now, you can **reference this output** in other scripts or Terraform configurations!

* * *

## **Chaining Outputs & Variables**

You can **use outputs as inputs** in another Terraform module. Let’s say you have **two Terraform configurations**, one that creates a **resource group** and another that provisions a **VM**.

1\. **First, define the output in `resource_group/outputs.tf`:**

```
output "resource_group_name" {
  value = azurerm_resource_group.example.name
}
```

2\. **Then, use it in another Terraform module (`virtual_machine/main.tf`):**

```
variable "resource_group_name" {}

resource "azurerm_virtual_machine" "example" {
  name                = "myVM"
  resource_group_name = var.resource_group_name
}
```

3\. **Pass the output as a variable:**

```bash
terraform apply -var="resource_group_name=$(terraform output -raw resource_group_name)"
```

**Result?** Fully **modular Terraform configurations** that pass values dynamically!

* * *

## **Best Practices for Using Variables & Outputs**

- **Use `.tfvars` files** for managing multiple environments.

- **Prefix variables with categories** (e.g., `network_subnet`, `app_instance_count`) to stay organized.

- **Never hardcode secrets!** Use Terraform's `sensitive = true` for secrets.

- **Output only what you need**—avoid exposing sensitive data.

* * *

## **Wrapping Up**

Variables and outputs are the **building blocks of scalable Terraform configurations**. They **reduce duplication, improve reusability, and make deployments more flexible**.

### **Quick Recap**

- **Variables** help you **parameterize your infrastructure**.

- **Outputs** let you **retrieve important resource details**.

- **Lists and Maps** allow you to **store multiple values efficiently**.

Now, go forth and **Terraform smarter, not harder**!

* * *

### **What’s Next?**

In the next post, we’ll explore **Terraform modules**—how to organize your infrastructure into reusable components. Stay tuned!
