// Miroir de Casino.Application.Tables.Contracts (backend).

export interface CreateTableResponse {
  code: string;
  adminToken: string;
}

export interface TableSummary {
  code: string;
  game: string;
  playerCount: number;
  maxPlayers: number;
}

export interface JoinTableRequest {
  name: string;
}

export interface JoinTableResponse {
  code: string;
  playerId: string;
  name: string;
  token: string;
}
