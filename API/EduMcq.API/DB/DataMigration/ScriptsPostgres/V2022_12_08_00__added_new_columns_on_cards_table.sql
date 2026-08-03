alter table cards add if not exists daily_limit_count_configured boolean default false;

alter table cards add if not exists daily_limit_amount_configured boolean default false;

alter table cards add if not exists daily_limit_cash_count_configured boolean default false;

alter table cards add if not exists daily_limit_cash_amount_configured boolean default false;