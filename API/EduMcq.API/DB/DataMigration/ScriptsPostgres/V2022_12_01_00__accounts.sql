create table if not exists accounts
(
    account_id                                       uuid    default gen_random_uuid() not null,
    institution_id                                   uuid                              not null,
    region_id                                        varchar(50),
    account_number                                   varchar(36)                       not null,
    billing_currency                                 char(3)                           not null,
    current_balance                                  numeric                           not null,
    pending_auth_amount                              numeric                           not null,
    credit_limit                                     numeric                           not null,
    cash_limit                                       numeric                           not null,
    cash_balance                                     numeric                           not null,
    active                                           boolean                           not null,
    product_id                                       uuid                              not null,
    program_manager_id                               uuid                              not null,
    emv_fallback_velocity                            smallint,
    emv_fallback_velocity_used                       smallint,
    currency_code_restriction_setup_configured       boolean,
    country_code_restriction_setup_configured        boolean,
    mcc_group_setup_configured                       boolean,
    pending_cash_auth_amount                         numeric,
    fraud_score_setup_configured                     boolean,
    rule_score_setup_configured                      boolean,
    token_transaction_setup_configured               boolean,
    single_transaction_limit                         numeric,
    single_cash_transaction_limit                    numeric,
    external_account_id                              varchar(250),
    last_delinquency_status                          boolean default false,
    restrict_spend_adc_status                        boolean default false,
    restrict_spend_setup_configured                  boolean default false,
    last_delinquency_date                            date,
    dynamic_data_element_setup_configured            boolean default false,
    use_plan_segment                                 boolean default false             not null,
    pending_credit                                   numeric,
    skip_adcs_status                                 boolean default false,
    skip_adcs_counter                                int4  default 0,
    account_manual_status                            varchar(30),
    status_restriction_configured                    boolean default false,
    main_status                                      varchar(100),    
    creation_date                                   timestamptz NULL,
    constraint account_external_account_id unique (external_account_id, program_manager_id),
    primary key (account_id)
);

create table if not exists account_adcs
(
    account_adc_id                varchar(36)  not null
        constraint "primary_account_adcs"
            primary key,
    account_id                    varchar(36)  not null,
    master_adc_id                 varchar(36)  not null,
    falcon_response_code          varchar(100) not null,
    continue_on_timeout           boolean      not null,
    active                        boolean      not null,
    mode                          varchar(25),
    description              varchar(50),
    program_manager_master_adc_id uuid         not null,
    registration_level            varchar default 'Account',
    constraint account_adcs_unique
        unique (program_manager_master_adc_id, account_id)
);

create index if not exists account_adcs_account_id_adc_id_key
    on account_adcs (account_id);

create table if not exists account_payments
(
    account_payment_id         uuid    default gen_random_uuid() not null
        constraint account_payments_pk
            primary key,
    account_id                 uuid                              not null,
    product_id                 uuid                              not null,
    program_manager_id         uuid                              not null,
    statement_date             timestamp,
    statement_balance          numeric,
    statement_paid_date        timestamp,
    statement_paid_amount      numeric,
    is_past_due                boolean default false,
    return_payment_date        timestamp,
    payment_mode               varchar(30),
    is_return_payment_occurred boolean,
    is_successful_payment      boolean default false,
    past_due_days              smallint,
    is_excess_payment          boolean,
    payment_hold_days          smallint,
    active                     boolean
);

