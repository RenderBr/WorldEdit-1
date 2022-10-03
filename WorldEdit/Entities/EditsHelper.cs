using Auxiliary;
using Auxiliary.Configuration;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorldEdit.Configuration;

namespace WorldEdit.Entities
{
    internal class EditsHelper
    {
        private static readonly Collection<EditsEntity> _client = new("WorldEdit");

        public static async Task<bool> ModifyAsync(EditsEntity user, UpdateDefinition<EditsEntity> update)
        {
            if (user.State is EntityState.Deserializing)
                return false;

            if (user.State is EntityState.Deleted)
                throw new InvalidOperationException($"{nameof(user)} cannot be modified post-deletion.");

            return await _client.ModifyDocumentAsync(user, update);
        }

        public static async Task<bool> DeleteAsync(EditsEntity user)
        {
            user.State = EntityState.Deleted;

            return await _client.DeleteDocumentAsync(user);
        }

        public static async Task<EditsEntity> GetAsync(int id)
        {
            var document = (await _client.FindDocumentAsync(x => x.TShockId == id)) ?? await CreateAsync(id);

            if (document is null)
                return null!;

            document.State = EntityState.Initialized;
            return document;
        }

        private static async Task<EditsEntity> CreateAsync(int tshockId)
        {
            var entity = new EditsEntity
            {
                TShockId = tshockId,
                RedoLevel = 0,
                UndoLevel = 0
            };

            await _client.InsertDocumentAsync(entity);
            return entity;
        }
    }
}
