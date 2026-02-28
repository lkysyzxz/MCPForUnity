#if UNITY_EDITOR
using System.Collections.Generic;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using ModelContextProtocol.Samples.CustomTypes;

namespace ModelContextProtocol.Editor
{
    public static partial class EditorToolsList
    {
        [McpServerTool("test_address", Description = "测试基础自定义类型 - 地址信息")]
        public static CallToolResult TestAddress(
            [McpArgument(Description = "地址信息", Required = true)] 
            AddressInfo address)
        {
            string result = $"地址: {address.Street}, {address.City}";
            if (!string.IsNullOrEmpty(address.ZipCode))
                result += $", 邮编: {address.ZipCode}";
            result += $", 国家: {address.Country}";

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = result }
                }
            };
        }

        [McpServerTool("test_person", Description = "测试嵌套自定义类型 - 人员信息")]
        public static CallToolResult TestPerson(
            [McpArgument(Description = "人员信息（包含嵌套地址）", Required = true)] 
            PersonInfo person)
        {
            string result = $"姓名: {person.Name}";
            if (person.Age > 0)
                result += $", 年龄: {person.Age}";
            if (!string.IsNullOrEmpty(person.Email))
                result += $", 邮箱: {person.Email}";
            if (person.Address != null)
                result += $"\n地址: {person.Address.Street}, {person.Address.City}";

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = result }
                }
            };
        }

        [McpServerTool("test_team", Description = "测试自定义类型数组 - 团队信息")]
        public static CallToolResult TestTeam(
            [McpArgument(Description = "团队信息（包含成员数组）", Required = true)] 
            TeamInfo team)
        {
            string result = $"团队: {team.TeamName}\n";
            result += $"成员数量: {team.Members?.Length ?? 0}\n";
            
            if (team.Members != null)
            {
                result += "成员列表:\n";
                foreach (var member in team.Members)
                {
                    result += $"  - {member.Name}";
                    if (!string.IsNullOrEmpty(member.Email))
                        result += $" ({member.Email})";
                    result += "\n";
                }
            }
            
            if (team.Tags != null && team.Tags.Count > 0)
            {
                result += $"标签: {string.Join(", ", team.Tags)}";
            }

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = result }
                }
            };
        }

        [McpServerTool("test_person_list", Description = "测试List<自定义类型>")]
        public static CallToolResult TestPersonList(
            [McpArgument(Description = "人员列表")] 
            List<PersonInfo> people)
        {
            string result = $"共 {people?.Count ?? 0} 人:\n";
            
            if (people != null)
            {
                for (int i = 0; i < people.Count; i++)
                {
                    result += $"{i + 1}. {people[i].Name}";
                    if (people[i].Age > 0)
                        result += $" ({people[i].Age}岁)";
                    result += "\n";
                }
            }

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = result }
                }
            };
        }

        [McpServerTool("test_invalid_type", Description = "测试非法自定义类型 - 不应被注册")]
        public static CallToolResult TestInvalidType(
            [McpArgument(Description = "非法类型")] 
            InvalidCustomType data)
        {
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = "不应该看到此消息 - 工具验证应该失败" }
                }
            };
        }
    }
}
#endif
