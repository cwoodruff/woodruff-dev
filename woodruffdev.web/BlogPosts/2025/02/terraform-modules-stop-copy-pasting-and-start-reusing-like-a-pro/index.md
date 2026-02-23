---
title: "Terraform Modules: Stop Copy-Pasting and Start Reusing Like a Pro"
date: 2025-02-21
categories: 
  - "terraform"
tags: 
  - "devops"
  - "iac"
  - "infrastructure"
  - "terraform"
coverImage: "Terraform-Modules-Stop-Copy-Pasting-and-Start-Reusing-Like-a-Pro.png"
---

Ever feel like you’re copy-pasting the same Terraform code over and over again? Deploying **resource groups, VMs, networks**—all with slightly different names? Yeah, that’s a nightmare waiting to happen.

Luckily, Terraform **modules** exist to make our lives easier! Modules let you **write infrastructure code once** and reuse it across different projects, environments, or even teams.

In this post, we’ll explore **how Terraform modules work, why they’re awesome, and how to create your own**. Let’s go!

* * *

## **Why Use Modules?**

Imagine you’re a **pizza chef**. Would you manually mix the dough, make the sauce, and prep toppings from scratch every single time? Or would you **use pre-made components** to speed things up?

Terraform modules are like **pre-made pizza bases**—they save time, enforce consistency, and make managing infrastructure a breeze.

- **Reuse configurations** across projects.

- **Reduce errors** by following best practices.

- **Make infrastructure modular** and easy to manage.

- **Keep code DRY** (Don’t Repeat Yourself).

* * *

## **Types of Terraform Modules**

There are **two main types** of Terraform modules:

1\. **Root Module** (your main Terraform configuration).  
2\. **Child Modules** (reusable components you call from the root module).

### **Where Can You Find Modules?**

- **Terraform Registry**: Pre-built modules you can use instantly → registry.terraform.io

- **Your Own Modules**: Custom modules specific to your organization.

* * *

## **Creating Your First Terraform Module**

Let’s say you want to create a **module for an Azure Resource Group** that can be reused across multiple environments.

### **Step 1: Create a Module Directory**

Inside your Terraform project, create a `modules/resource_group` folder:

```bash
mkdir -p modules/resource_group
```

### **Step 2: Define the Resource (`main.tf`)**

Inside `modules/resource_group/main.tf`, define a resource group:

```
resource "azurerm_resource_group" "rg" {
  name     = var.resource_group_name
  location = var.location
}
```

### **Step 3: Define Input Variables (`variables.tf`)**

```
variable "resource_group_name" {
  description = "The name of the resource group"
  type        = string
}

variable "location" {
  description = "The Azure region"
  type        = string
  default     = "East US"
}
```

### **Step 4: Define Outputs (`outputs.tf`)**

```
output "resource_group_name" {
  value = azurerm_resource_group.rg.name
}
```

* * *

## **Using the Module in Your Main Terraform Config**

Now, let’s call this module from our **root Terraform configuration** (`main.tf`):

```
module "my_resource_group" {
  source              = "./modules/resource_group"
  resource_group_name = "TerraformRG"
  location           = "West US"
}
```

Now when you run `terraform apply`, Terraform will **call the module** and create the resource group dynamically!

* * *

## **Working with Public Modules**

Want to **reuse** someone else’s module? No problem! Terraform has a **public registry** with pre-built modules.

For example, to create an **Azure Storage Account**, you can use a module from the Terraform Registry:

```
module "storage_account" {
  source              = "Azure/azurerm/terraform"
  version             = "3.0.0"
  storage_account_name = "myterraformstorage"
  location           = "East US"
}
```

That’s it—Terraform **fetches the module, configures it, and deploys** it for you.

* * *

## **Best Practices for Terraform Modules**

- **Keep modules small** – Each module should handle **one** task (e.g., VMs, networks).

- **Use input variables** – Allow customization when calling the module.

- **Define outputs** – Make it easy to pass values between modules.

- **Store modules in Git** – Version control your infrastructure just like application code.

- **Use the Terraform Registry** – Don’t reinvent the wheel if a module already exists.

* * *

## **Wrapping Up**

Terraform modules are **game-changers** when it comes to managing infrastructure efficiently. They **save time, improve code quality, and keep your configurations modular**.

**Quick Recap:**

- **Modules = Reusable Terraform code**

- **Use input variables & outputs**

- **Call modules from your root Terraform config**

- **Use the Terraform Registry for pre-built solutions**

Now, go forth and **modularize your Terraform projects**!

* * *

### **What’s Next?**

In the next post, we’ll dive into **Advanced Terraform Features** like **provisioners, functions, and workspaces** to level up your infrastructure skills. Stay tuned!
