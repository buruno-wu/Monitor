﻿using Dapper;
using IntelligentMonitor.Models.AppSettings;
using IntelligentMonitor.Utility;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace IntelligentMonitor.Providers.Users
{
    using IntelligentMonitor.Models.Users;

    public class UserProvider
    {
        private readonly AppSettings _settings;
        private readonly IntelligentMonitorContext _context;

        public UserProvider(AppSettings settings, IntelligentMonitorContext context)
        {
            _settings = settings;
            _context = context;
        }

        /// <summary>
        /// 添加用户
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<int> InsertUser(Users user)
        {
            var sql = @"INSERT INTO users ( UserName, NickName, Password, RoleId, IsDelete )
                        VALUES ( @UserName, @NickName, @Password, @RoleId, 0 )";
            using (IDbConnection conn = _settings.MySqlConnection)
            {
                return await conn.ExecuteAsync(sql, user);
            }
        }

        /// <summary>
        /// 修改用户
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<int> UpdateUser(Users user)
        {
            var sql = @"UPDATE users 
                        SET NickName = @NickName,
                        Password = @Password,
                        RoleId = @RoleId
                        WHERE Id = @Id";
            using (IDbConnection conn = _settings.MySqlConnection)
            {
                return await conn.ExecuteAsync(sql, user);
            }
        }

        /// <summary>
        /// 验证密码
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<bool> VerifyPassword(int Id, string password)
        {
            var sql = "SELECT COUNT(*) FROM users WHERE Password = @password and Id = @Id";
            using (IDbConnection conn = _settings.MySqlConnection)
            {
                return await conn.ExecuteScalarAsync<int>(sql, new { Id, password = MD5Util.TextToMD5(password) }) > 0;
            }
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public async Task<int> DeleteUser(int Id)
        {
            var sql = @"UPDATE users 
                        SET IsDelete = 1
                        WHERE Id = @Id";
            using (IDbConnection conn = _settings.MySqlConnection)
            {
                return await conn.ExecuteAsync(sql, new { Id });
            }
        }

        /// <summary>
        /// 用户列表
        /// </summary>
        /// <returns></returns>
        public List<Users> GetUserList()
        {
            var sql = @"SELECT
	                    u.Id,
	                    u.UserName,
	                    u.NickName,
	                    u.RoleId,
	                    r.RoleName 
                    FROM
	                    users AS u
	                    JOIN roles AS r ON u.RoleId = r.Id 
                    WHERE
                        u.IsDelete = 0";
            using (IDbConnection conn = _settings.MySqlConnection)
            {
                return conn.Query<Users>(sql).ToList();
            }
        }

        /// <summary>
        /// 根据Id获取用户
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public Users GetUser(int Id)
        {
            var sql = @"SELECT
	                    u.Id,
	                    u.UserName,
	                    u.NickName,
	                    u.PASSWORD,
	                    u.RoleId,
	                    r.RoleName 
                    FROM
	                    users AS u
	                    JOIN roles AS r ON u.RoleId = r.Id 
                    WHERE
                        u.IsDelete = 0
	                    AND u.Id = @Id";
            using (IDbConnection conn = _settings.MySqlConnection)
            {
                var list = new List<PermissionViewModel>();

                var user = conn.Query<Users>(sql, new { Id }).FirstOrDefault();

                var permissionList = _context.Permissions.Where(p => p.RoleId == user.RoleId).ToList();
                permissionList.ForEach(x =>
                {
                    list.Add(new PermissionViewModel { UserId = user.Id, PermissionName = x.PermissionName });
                });
                user.Permissions = list;

                return user;
            }
        }

        /// <summary>
        /// 根据账号密码获取用户
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public Users GetUser(string username, string password)
        {
            var sql = @"SELECT
	                    u.Id,
	                    u.UserName,
	                    u.NickName,
	                    u.PASSWORD,
	                    u.RoleId,
	                    r.RoleName 
                    FROM
	                    users AS u
	                    JOIN roles AS r ON u.RoleId = r.Id 
                    WHERE
	                    u.IsDelete = 0 
	                    AND u.UserName = @username
	                    AND u.Password = @password";
            using (IDbConnection conn = _settings.MySqlConnection)
            {
                var user = conn.Query<Users>(sql, new { username, password = MD5Util.TextToMD5(password) }).FirstOrDefault();

                return user;
            }
        }

        /// <summary>
        /// 检查权限
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="permissionName"></param>
        /// <returns></returns>
        public bool CheckPermission(int Id, string permissionName)
        {
            var user = GetUser(Id);
            if (user == null)
            {
                return false;
            }
            return user.Permissions.Any(p => permissionName.StartsWith(p.PermissionName));
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public async Task<int> DeleteRole(int Id)
        {
            var sql = @"DELETE FROM roles WHERE Id = @Id";
            using (IDbConnection conn = _settings.MySqlConnection)
            {
                return await conn.ExecuteAsync(sql, new { Id });
            }
        }

        /// <summary>
        /// 角色列表
        /// </summary>
        /// <returns></returns>
        public List<Roles> GetRoleList()
        {
            return _context.Roles.ToList();
        }

        /// <summary>
        /// 添加权限
        /// </summary>
        /// <param name="permission"></param>
        /// <returns></returns>
        public async Task<int> InsertPermission(Permissions permission)
        {
            var sql = "INSERT INTO permissions(RoleId,PermissionName,PermissionDescribe) VALUES(@RoleId, @PermissionName,@PermissionDescribe)";
            using (IDbConnection conn = _settings.MySqlConnection)
            {
                return await conn.ExecuteAsync(sql, permission);
            }
        }

        /// <summary>
        /// 删除权限
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public async Task<int> DeletePermissionByRoleId(int Id)
        {
            var sql = @"DELETE FROM permissions WHERE RoleId = @Id";
            using (IDbConnection conn = _settings.MySqlConnection)
            {
                return await conn.ExecuteAsync(sql, new { Id });
            }
        }

        /// <summary>
        /// 权限列表 
        /// </summary>
        /// <returns></returns>
        public List<Permissions> GetPermissionList()
        {
            return _context.Permissions.ToList();
        }
    }
}