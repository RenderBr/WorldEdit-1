using Auxiliary;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldEdit.Entities
{
    [BsonIgnoreExtraElements]
    public class EditsEntity : IEntity, IConcurrentlyAccessible<EditsEntity>
    {
        [BsonId]
        public ObjectId ObjectId { get; set; }

        [BsonIgnore]
        public EntityState State { get; set; }

        private int _tshockId;
        public int TShockId
        {
            get
                => _tshockId;
            set
            {
                _ = ModifyAsync(Builders<EditsEntity>.Update.Set(x => x.TShockId, value));
                _tshockId = value;
            }
        }

        private int _undoAmount;
        public int UndoLevel
        {
            get
                => _undoAmount;
            set
            {
                _ = ModifyAsync(Builders<EditsEntity>.Update.Set(x => x.UndoLevel, value));
                _undoAmount = value;
            }
        }

        private int _redoAmount;
        public int RedoLevel
        {
            get
                => _redoAmount;
            set
            {
                _ = ModifyAsync(Builders<EditsEntity>.Update.Set(x => x.RedoLevel, value));
                _redoAmount = value;
            }
        }

        public async Task<bool> DeleteAsync()
            => await EditsHelper.DeleteAsync(this);

        public async Task<bool> ModifyAsync(UpdateDefinition<EditsEntity> update)
            => await EditsHelper.ModifyAsync(this, update);

        public static async Task<EditsEntity> GetAsync(int id)
            => await EditsHelper.GetAsync(id);

        public void Dispose()
        {

        }
    }
}
