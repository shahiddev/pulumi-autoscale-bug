// Copyright 2016-2020, Pulumi Corporation.  All rights reserved.

using System.Collections.Generic;
using System.Linq;
using Pulumi;
using Pulumi.Azure.AppInsights;
using Pulumi.Azure.AppService;
using Pulumi.Azure.AppService.Inputs;
using Pulumi.Azure.Core;
using Pulumi.Azure.Monitoring;
using Pulumi.Azure.Monitoring.Inputs;
using Pulumi.Azure.Sql;
using Pulumi.Azure.Storage;

class AppServiceStack : Stack
{
    public AppServiceStack()
    {
        var resourceGroup = new ResourceGroup("appservice-rg");

        var appServicePlan = new Plan("asp", new PlanArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Kind = "App",
            Sku = new PlanSkuArgs
            {
                Tier = "Basic",
                Size = "B1",
            },
        });

        var sites = new List<AppService>();

       var siteNames = new[]{"site1"};
       

       foreach (var siteName in siteNames)
       {
        var app = new AppService(siteName, new AppServiceArgs
        {
            ResourceGroupName = resourceGroup.Name,
            AppServicePlanId = appServicePlan.Id,
            
        });
        var autoscale = new AutoscaleSetting($"as-{siteName}", new AutoscaleSettingArgs
            {
                ResourceGroupName = resourceGroup.Name,
                Enabled = true,
                TargetResourceId = appServicePlan.Id,
                Notification = new AutoscaleSettingNotificationArgs
                {
                    Email = new AutoscaleSettingNotificationEmailArgs
                    {
                        SendToSubscriptionAdministrator = false,
                        SendToSubscriptionCoAdministrator = false,
                        CustomEmails = { { "someone@email.com" } }
                    }
                },
                Profiles = 
                    {
                        new AutoscaleSettingProfileArgs
                            {
                                Name = "auto scale function",
                                Capacity = new AutoscaleSettingProfileCapacityArgs { Minimum = 1, Maximum = 2, Default = 1 },
                                Rules =
                                {
                                    //scale up
                                    new AutoscaleSettingProfileRuleArgs
                                    {
                                        MetricTrigger = new AutoscaleSettingProfileRuleMetricTriggerArgs
                                        {
                                            MetricName = "CpuPercentage",
                                            MetricResourceId = appServicePlan.Id,
                                            Operator = "GreaterThan",
                                            Statistic= "Average",
                                            Threshold = 80.0,
                                            TimeAggregation = "Average",
                                            TimeGrain = "PT1M",
                                            TimeWindow = "PT10M"
                                        },
                                        ScaleAction = new AutoscaleSettingProfileRuleScaleActionArgs
                                        {
                                            Cooldown = "PT5M",
                                            Direction = "Increase",
                                            Type = "ChangeCount",
                                            Value = 1
                                        }
                                    },
                                    //scale down
                                    new AutoscaleSettingProfileRuleArgs
                                    {
                                        MetricTrigger = new AutoscaleSettingProfileRuleMetricTriggerArgs
                                        {
                                            MetricName = "CpuPercentage",
                                            MetricResourceId = appServicePlan.Id,
                                            Operator = "LessThan",
                                            Statistic= "Average",
                                            Threshold = 80.0,
                                            TimeAggregation = "Average",
                                            TimeGrain = "PT1M",
                                            TimeWindow = "PT10M"
                                        },
                                        ScaleAction = new AutoscaleSettingProfileRuleScaleActionArgs
                                        {
                                            Cooldown = "PT5M",
                                            Direction = "Decrease",
                                            Type = "ChangeCount",
                                            Value = 1
                                        }
                                    },
                                }
                            }
                    }   
            });

            sites.Add(app);
        } 

        this.Endpoint = sites.First().DefaultSiteHostname;
    }

    [Output] public Output<string> Endpoint { get; set; }
}
