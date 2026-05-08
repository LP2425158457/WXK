**一：编写执行计划插件**

1\. 必须继承Kingdee.BOS.Contracts.IScheduleService.cs接口

2\. 实现里面的void Run(Context ctx,Schedule schedule)

3\. 编译成组件放到bin目录下

4\. 插件代码框架，如下

```C#
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Kingdee.BOS.Core;
using Kingdee.BOS.Contracts;

namespace Kingdee.Schedule.Test
{
    /// <summary>
    /// 执行计划：自定义执行计划执行计划
    /// </summary>
    [Description("自定义执行计划执行计划")]
    public class CustomerSchedule : IScheduleService
    {
        /// <summary>
        /// 自动计划，执行入口
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="schedule"></param>
        public void Run(Context ctx, Schedule schedule)
        {
          //实现业务代码
        }
    }
}
```

