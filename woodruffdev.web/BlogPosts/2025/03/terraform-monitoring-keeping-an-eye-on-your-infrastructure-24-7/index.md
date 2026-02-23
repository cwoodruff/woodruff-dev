---
title: "Terraform + Monitoring: Keeping an Eye on Your Infrastructure 24/7"
date: 2025-03-05
categories: 
  - "terraform"
tags: 
  - "devops"
  - "iac"
  - "infrastructure"
  - "terraform"
coverImage: "Terraform-Monitoring-Keeping-an-Eye-on-Your-Infrastructure-247.png"
---

You’ve **built your cloud infrastructure with Terraform**—awesome! But now what? How do you know if your **VMs are running smoothly**? What if your **databases are overloading** or your **Kubernetes cluster is on fire**?

Infrastructure isn’t **set it and forget it**—you need **real-time monitoring** to catch issues **before users notice**.

**Good news:** Terraform can **deploy and configure monitoring tools** so you can **track metrics, set up alerts, and visualize performance** effortlessly!

In this post, we’ll cover:

- **Why monitoring Terraform-managed infrastructure is crucial.**

- **How to set up monitoring in AWS, Azure, and GCP.**

- **Using Grafana, Prometheus, and other Terraform-powered monitoring tools.**

- **Setting up alerts so you know when things go wrong.**

Let’s get your **Terraform infrastructure under 24/7 surveillance!**

* * *

## **1\. Why Monitor Terraform-Provisioned Infrastructure?**

Terraform is **great at deploying infrastructure**, but once resources are live, Terraform **doesn’t manage their health**.

Without monitoring, you risk:

- **Unexpected downtime** because you didn’t track resource failures.

- **Over-provisioned resources** leading to wasted cloud spend.

- **Security vulnerabilities** due to missing audit logs.

**With monitoring, you can detect failures early, optimize performance, and reduce costs.**

* * *

## **2\. Deploying Cloud Monitoring with Terraform**

Most cloud providers have **built-in monitoring tools**. Terraform can configure them automatically!

* * *

### **AWS: Terraform + CloudWatch for Logs & Metrics**

AWS **CloudWatch** tracks logs, metrics, and alerts for your resources. Let’s configure Terraform to **monitor an EC2 instance**.

#### **Step 1: Enable CloudWatch Monitoring for an EC2 Instance**

```
resource "aws_instance" "web" {
  ami                    = "ami-123456"
  instance_type          = "t2.micro"
  monitoring             = true  # Enables detailed monitoring
}
```

#### **Step 2: Create a CloudWatch Alarm for High CPU Usage**

```
resource "aws_cloudwatch_metric_alarm" "high_cpu" {
  alarm_name          = "HighCPUAlarm"
  comparison_operator = "GreaterThanThreshold"
  threshold           = 80
  evaluation_periods  = 2
  metric_name         = "CPUUtilization"
  namespace           = "AWS/EC2"
  period              = 60
  statistic           = "Average"
  alarm_actions       = ["arn:aws:sns:us-east-1:123456789012:alerts"]
}
```

**Now, if CPU usage exceeds 80%, Terraform triggers an alarm!**

* * *

### **Azure: Terraform + Azure Monitor**

Azure Monitor collects **logs and metrics** for **VMs, databases, and network traffic**. Let’s set up Terraform to **monitor an Azure VM**.

#### **Step 1: Enable Monitoring for an Azure VM**

```
resource "azurerm_monitor_diagnostic_setting" "vm_monitor" {
  name                           = "vm-monitor"
  target_resource_id             = azurerm_virtual_machine.example.id
  log_analytics_workspace_id     = azurerm_log_analytics_workspace.example.id

  log {
    category = "Administrative"
    enabled  = true
  }

  metric {
    category = "AllMetrics"
    enabled  = true
  }
}
```

#### **Step 2: Set Up an Alert for High Memory Usage**

```
resource "azurerm_monitor_metric_alert" "high_memory" {
  name                = "HighMemoryUsage"
  resource_group_name = azurerm_resource_group.example.name
  scopes             = [azurerm_virtual_machine.example.id]
  criteria {
    metric_name      = "Percentage CPU"
    aggregation      = "Average"
    operator         = "GreaterThan"
    threshold        = 85
  }
}
```

**Terraform now monitors Azure VMs and triggers alerts when memory usage is high!**

* * *

### **GCP: Terraform + Stackdriver (Cloud Monitoring)**

Google’s **Cloud Monitoring (Stackdriver)** collects logs and metrics across GCP services.

#### **Step 1: Enable Cloud Monitoring for a GCP VM**

```
resource "google_monitoring_dashboard" "vm_dashboard" {
  dashboard_json = <<EOT
{
  "displayName": "VM Monitoring",
  "gridLayout": {
    "widgets": [
      {
        "title": "CPU Usage",
        "xyChart": {
          "dataSets": [
            {
              "timeSeriesQuery": {
                "timeSeriesFilter": {
                  "filter": "metric.type=\"compute.googleapis.com/instance/cpu/utilization\"",
                  "aggregation": { "alignmentPeriod": "60s" }
                }
              }
            }
          ]
        }
      }
    ]
  }
}
EOT
}
```

**Terraform now sets up a GCP dashboard to track CPU utilization!**

* * *

## **3\. Visualizing Terraform Infrastructure with Grafana + Prometheus**

Terraform can **deploy monitoring dashboards** with **Grafana and Prometheus**, giving you **real-time insights** into your cloud infrastructure.

### **Deploy Grafana with Terraform**

```
resource "aws_instance" "grafana" {
  ami             = "ami-123456"
  instance_type   = "t2.micro"
  security_groups = [aws_security_group.grafana.name]

  user_data = <<-EOF
    #!/bin/bash
    sudo apt-get update -y
    sudo apt-get install -y grafana
    sudo systemctl start grafana-server
    sudo systemctl enable grafana-server
  EOF
}
```

**Now, Grafana is deployed and ready to monitor Terraform resources!**

* * *

## **4\. Alerting: Get Notified When Things Go Wrong**

What’s the point of monitoring if **no one sees alerts**? Terraform can **send notifications to Slack, email, or PagerDuty** when infrastructure fails.

### **Example: AWS CloudWatch Alarm Sending Alerts to Slack**

```
resource "aws_sns_topic" "alerts" {
  name = "cloudwatch-alerts"
}

resource "aws_sns_topic_subscription" "slack_alerts" {
  topic_arn = aws_sns_topic.alerts.arn
  protocol  = "https"
  endpoint  = "https://hooks.slack.com/services/T00000000/B00000000/XXXXXXXXXXXXXXXXXXXXXXXX"
}
```

**Now, Terraform alerts go straight to Slack!**

* * *

## **5\. Common Monitoring Mistakes & Fixes**

| **Mistake** | **Fix** |
| --- | --- |
| Not enabling **detailed monitoring** | Use Terraform to enable monitoring when deploying resources. |
| No alerting configured | Set up **SNS, Slack, or PagerDuty notifications**. |
| Manual setup of dashboards | Deploy **Grafana dashboards** with Terraform. |

**Pro Tip:** Always monitor **cost usage**—Terraform can also track cloud expenses!

* * *

## **Wrapping Up**

Terraform doesn’t just **provision infrastructure**—it can also **set up monitoring and alerting** so your cloud stays **healthy and secure**.

**Quick Recap:**

- **Use Terraform to configure AWS CloudWatch, Azure Monitor, and GCP Stackdriver.**

- **Deploy Grafana and Prometheus for real-time dashboards.**

- **Set up alerts for CPU, memory, and network spikes.**

- **Send notifications via Slack, email, or PagerDuty.**

Now, go **Terraform your monitoring stack** and **watch your infrastructure in action!**

* * *

### **What’s Next?**

Terraform has a vast ecosystem of tools that make infrastructure automation even better. In the next post, **"Terraform Ecosystem Tools,"** we’ll explore **Terragrunt, Atlantis, OpenTofu, and other powerful Terraform extensions**.
